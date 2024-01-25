using Microsoft.Extensions.Configuration;
using Pricing_Engine.Model;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Context.Propagation;
using System.Diagnostics;
using OpenTelemetry;
using Pricing_Engine.Helper;

namespace Pricing_Engine.MessageHandler
{
    internal class PricingMessageHandler : IMessageHandler
    {
        private readonly int _batchSize;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly string _cartStream;
        private readonly string _adminServiceUrl;
        private readonly ILogger<PricingMessageHandler> _logger;
        private readonly ActivitySource _activitySource = new(Instrumentation.ActivitySourceName);
        private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;

        public PricingMessageHandler(ILogger<PricingMessageHandler> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _batchSize = _configuration.GetValue<int>("BatchSize");
            _cartStream = _configuration.GetValue<string>("CartStream") ?? "cart-stream";
            _adminServiceUrl = _configuration.GetValue<string>("AdiminServiceUrl") ?? "https://localhost:7190/api";
            _logger = logger;
        }
        public async Task HandleMessage(List<CartMessage> cartMessage, IDatabase database)
        {
            var productList = new ConcurrentBag<PriceListItemData>();
            var pricingResponseList = new ConcurrentBag<PricingResponse>();
            await Parallel.ForEachAsync(cartMessage, async (message, _) =>
            {
                var parentContext = Propagator.Extract(default, message, InstrumentationHelper.ExtractTraceContextFromBasicProperties);
                Baggage.Current = parentContext.Baggage;
                using var activity = _activitySource.StartActivity(nameof(HandleMessage), ActivityKind.Internal, parentContext.ActivityContext);
                {
                    var cartId = message.CartId;
                    var cartItems = message.CartItems.ToList();
                    var iteration = Math.Ceiling((decimal)cartItems.Count / _batchSize);
                    var httpTasks = new List<Task<HttpResponseMessage>>();
                    _logger.LogInformation($"started processing a message  for cart Id: {message.CartId} at {DateTime.Now}");
                    using var activity1 = _activitySource.StartActivity($"{nameof(HandleMessage)} : Get price list", ActivityKind.Internal);
                    {
                        using HttpClient client = _httpClientFactory.CreateClient();
                        for (var i = 0; i < iteration; i++)
                        {
                            var itemIds = cartItems.Skip(i * _batchSize).Take(_batchSize).Select(x => x.Product.Id);
                            var priceListItemRequest = new PriceListItemQuery
                            {
                                Ids = itemIds
                            };
                            var json = new StringContent(
                                    JsonSerializer.Serialize(priceListItemRequest, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
                                    Encoding.UTF8,
                                    MediaTypeNames.Application.Json);
                            httpTasks.Add(client.PostAsync($"{_adminServiceUrl}/pricelist/{message.PriceListId}/pricelistitems/query", json));
                        }
                        while (httpTasks.Any())
                        {
                            var completedTask = await Task.WhenAny(httpTasks);
                            httpTasks.Remove(completedTask);

                            var httpResponse = await completedTask;
                            if (httpResponse != null && httpResponse.IsSuccessStatusCode)
                            {
                                var options = new JsonSerializerOptions
                                {
                                    Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
                                    PropertyNameCaseInsensitive = true
                                };
                                var response = await JsonSerializer.DeserializeAsync<ApiResponse<List<PriceListItemData>>>(await httpResponse.Content.ReadAsStreamAsync(), options);
                                if (response != null)
                                {
                                    response.Data.ForEach(x => { productList.Add(x); });
                                }
                            }
                            else
                            {
                                _logger.LogInformation($"API call to admin service has failed {httpResponse.StatusCode}");
                            }
                        }
                    }
                    var priceResponse = CalculateCartPrice(productList, message);
                    _logger.LogInformation($"Pricing   for cart Id: {message.CartId} is completed at {DateTime.Now}");
                    if (priceResponse != null && priceResponse.CartItems.Any())
                    {
                        _logger.LogInformation($"Pricing for total items {priceResponse.CartItems.Count} is done for the cart id {priceResponse.CartId}");
                        await SendToCart(priceResponse, database);
                    }
                }
            });
        }


        // TODO: This takes lot of time when number of lines goes up,
        // Can we parallalize with splitting of data?
        // else spit after config-engine configures and send them to different pricing-engine pods
        // so that all the thing related to prcing is done parallally.
        private PricingResponse CalculateCartPrice(ConcurrentBag<PriceListItemData> products, CartMessage cartMessage)
        {
            _logger.LogInformation($"Pricing started   for cart Id: {cartMessage.CartId}  {DateTime.Now}");
            var lineItem = new ConcurrentBag<CartItem>();
            using (var activity = _activitySource.StartActivity(nameof(CalculateCartPrice), ActivityKind.Internal))
            {
                var pricingResponse = new PricingResponse(cartMessage.CartId);
                double totalPrice = 0.00;
                foreach (var item in cartMessage.CartItems)
                {
                    var product = products.FirstOrDefault(x => x.ProductId == item.Product.Id);
                    if (product != null)
                    {
                        var itemPrice = item.Quantity * product.Price;
                        totalPrice += itemPrice;
                        pricingResponse.CartItems.Add(new CartItem
                        {
                            Price = itemPrice,
                            Currency = product.Currency,
                            CartItemId = item.ItemId
                        });
                    }
                    else
                    {
                        _logger.LogInformation($" Cart item: {item.ItemId} and product id {item.Product.Id} is not available");
                    }

                }
                pricingResponse.TotalPrice = totalPrice;
                pricingResponse.PriceListId = cartMessage.PriceListId;
                return pricingResponse;

                //Parallel.ForEach<CartItemRequest, double>(
                //    cartMessage.CartItems, // source collection
                //    () => 0.00, // method to initialize the local variable
                //    (item, loop, subtotal) => // method invoked by the loop on each iteration
                //    {
                //        var product = products.FirstOrDefault(x => x.ProductId == item.Product.Id);
                //        var itemPrice = item.Quantity * product.Price;
                //        lineItem.Add(new CartItem
                //        {
                //            Price = itemPrice,
                //            Currency = product.Currency,
                //            CartItemId = item.ItemId
                //        });
                //        subtotal += product.Price; //modify local variable
                //        return subtotal!; // value to be passed to next iteration
                //    },
                //    // Method to be executed when each partition has completed.
                //    // finalResult is the final value of subtotal for a particular partition.
                //    (finalResult) => totalPrice+= finalResult);

                //Parallel.ForEach(cartMessage.CartItems, new ParallelOptions
                //{
                //    // multiply the count because a processor has 2 cores
                //    MaxDegreeOfParallelism = Convert.ToInt32(Math.Ceiling(Environment.ProcessorCount * 0.75))
                //},
                //(item) =>
                //{
                //    var product = products.FirstOrDefault(x => x.ProductId == item.Product.Id);
                //    if (product != null)
                //    {
                //        lineItem.Add(new CartItem
                //        {
                //            Price = item.Quantity * product.Price,
                //            Currency = product.Currency,
                //            CartItemId = item.ItemId
                //        });
                //    }
                //    else
                //    {
                //        _logger.LogInformation($" Cart item: {item.ItemId} and product id {item.Product.Id} is not available");
                //    }
                //});
                //foreach (var item in lineItem)
                //{
                //    pricingResponse.TotalPrice += item.Price;
                //}
                //pricingResponse.PriceListId = cartMessage.PriceListId;
                //pricingResponse.CartItems = lineItem.ToList();
                //return pricingResponse;

            }
        }

        private async Task SendToCart(PricingResponse message, IDatabase database)
        {
            using (var activity = _activitySource.StartActivity("Send To Cart-worker", ActivityKind.Internal))
            {
                InstrumentationHelper.AddActivityToRequest(activity, message, "Pricing-engine", "PublishMessage");
                var nameValueEntry = new NameValueEntry[]
                {
                    new NameValueEntry(nameof(message),JsonSerializer.Serialize(message))
                };

                await database.StreamAddAsync(_cartStream, nameValueEntry);
                _logger.LogInformation($"Pricing is completed for  cart ID: {message.CartId} and sent to cart worker at: {DateTime.Now}");
            }
        }
    }
}
