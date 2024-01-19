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

        public PricingMessageHandler(ILogger<PricingMessageHandler> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _batchSize = 5000;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _cartStream = _configuration.GetValue<string>("CartStream") ?? "cart-stream";
            _adminServiceUrl = _configuration.GetValue<string>("AdiminServiceUrl") ?? "https://localhost:7190/api";
            _logger = logger;
        }
        public async Task HandleMessage(List<CartMessage> cartMessage, IDatabase database)
        {
            var productList = new ConcurrentBag<ProductData>();
            //foreach (var message in cartMessage)
            //{               
            //    var cartId = message.CartId;
            //    var cartItems = message.CartItems.ToList();
            //    var iteration = Math.Ceiling((decimal)cartItems.Count / _batchSize);
            //    var httpTasks = new List<Task<HttpResponseMessage>>();
            //    _logger.LogInformation($"started processing a message  for cart Id: {message.CartId} at {DateTime.Now}");
            //    using HttpClient client = _httpClientFactory.CreateClient();
            //    for (var i = 0; i < iteration; i++)
            //    {
            //        var itemIds = cartItems.Skip(i).Take(_batchSize).Select(x => x.Product.Id.ToString());
            //        var json = new StringContent(
            //                JsonSerializer.Serialize(itemIds, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            //                Encoding.UTF8,
            //                MediaTypeNames.Application.Json);
            //        httpTasks.Add(client.PostAsync($"{_adminServiceUrl}/product/query", json));
            //    }
            //    while (httpTasks.Any())
            //    {
            //        var completedTask = await Task.WhenAny(httpTasks);
            //        httpTasks.Remove(completedTask);

            //        var httpResponse = await completedTask;
            //        if (httpResponse != null && httpResponse.IsSuccessStatusCode)
            //        {
            //            var options = new JsonSerializerOptions
            //            {
            //                Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
            //                PropertyNameCaseInsensitive = true
            //            };
            //            var response = await JsonSerializer.DeserializeAsync<ApiResponse<List<ProductData>>>(httpResponse.Content.ReadAsStream(), options)!;
            //            pricingList.Add(await CalculateCartPrice(response!.Data, message));

            //        }
            //    }
            //    if (pricingList.Any())
            //    {
            //        var pricingResponse = new PricingResponse(message.CartId);
            //        foreach (var item in pricingList)
            //        {
            //            pricingResponse.CartItems.AddRange(item.CartItems);
            //        }
            //        _logger.LogInformation($"Total records priced: {pricingResponse.CartItems.Count(x => x.Price > 0)}");
            //        await SendToCart(pricingResponse, database);
            //    }
            //}
            await Parallel.ForEachAsync(cartMessage, async (message, _) =>
            {
                var cartId = message.CartId;
                var cartItems = message.CartItems.ToList();
                var iteration = Math.Ceiling((decimal)cartItems.Count / _batchSize);
                var httpTasks = new List<Task<HttpResponseMessage>>();
                _logger.LogInformation($"started processing a message  for cart Id: {message.CartId} at {DateTime.Now}");
                using HttpClient client = _httpClientFactory.CreateClient();
                for (var i = 0; i < iteration; i++)
                {
                    var itemIds = cartItems.Skip(i).Take(5000).Select(x => x.Product.Id);
                    var json = new StringContent(
                            JsonSerializer.Serialize(itemIds, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
                            Encoding.UTF8,
                            MediaTypeNames.Application.Json);
                    httpTasks.Add(client.PostAsync($"{_adminServiceUrl}/product/query", json));
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
                        var response = await JsonSerializer.DeserializeAsync<ApiResponse<List<ProductData>>>(httpResponse.Content.ReadAsStream(), options)!;
                        if (response != null)
                        {
                            response.Data.ForEach(x => { productList.Add(x); });
                        }
                    }
                }
                var itemlist = productList.Select(x => x.ProductId).OrderBy(y => y).ToList();
                await File.AppendAllTextAsync(@"C:\Users\vbhat\source\repos\Pricing-Engine\bin\Debug\net7.0\test1.txt", string.Join(",", itemlist));
                var priceResponse = CalculateCartPrice(productList, message);
                _logger.LogInformation($"Pricing   for cart Id: {message.CartId} is completed at {DateTime.Now}");
                if (priceResponse != null && priceResponse.CartItems.Any())
                {
                    _logger.LogInformation($"Pricing for total items {priceResponse.CartItems.Count} is done for the cart id {priceResponse.CartId}");
                    await SendToCart(priceResponse, database);
                }
            });
        }

        private PricingResponse CalculateCartPrice(ConcurrentBag<ProductData> products, CartMessage cartMessage)
        {
            var pricingResponse = new PricingResponse(cartMessage.CartId);
            var totalPrice = 0.00;
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
        }

        private async Task SendToCart(PricingResponse message, IDatabase database)
        {
            var nameValueEntry = new NameValueEntry[]
            {
                new NameValueEntry(nameof(message),JsonSerializer.Serialize(message))
            };

            await database.StreamAddAsync(_cartStream, nameValueEntry);
            _logger.LogInformation($"Pricing is completed for  cart ID: {message.CartId} and sent to cart worker at: {DateTime.Now}");
        }
    }
}
