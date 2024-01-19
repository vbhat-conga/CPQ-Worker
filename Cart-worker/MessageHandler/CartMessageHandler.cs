using Cart_Worker.Model;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;
using System.Collections.Concurrent;
using System.Net.Mime;
using System.Text.Json;
using System.Text;
using Cart_worker.Model;
using Microsoft.Extensions.Logging;

namespace Cart_Worker.MessageHandler
{
    internal class CartMessageHandler : IMessageHandler
    {
        private readonly int _batchSize;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly string _cartServiceUrl;
        private readonly ILogger<CartMessageHandler> _logger;

        public CartMessageHandler(ILogger<CartMessageHandler> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _batchSize = 5000;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _cartServiceUrl = _configuration.GetValue<string>("CartUrl") ?? "https://localhost:7036/api";
        }
        public async Task HandleMessage(List<PricingResponse> pricingResponses, IDatabase database)
        {
            _logger.LogInformation($"started processing a message at {DateTime.Now}");
            var cartItemUpdateRequest = new ConcurrentBag<CartItemUpdateRequest>();
            //foreach (var message in pricingResponses)
            //{
            //    var httpTasks = new List<Task<HttpResponseMessage>>();
            //    var iteration = Math.Ceiling((decimal)message.CartItems.Count / _batchSize);
            //    using HttpClient client = _httpClientFactory.CreateClient();         
            //    for (var i = 0; i < iteration; i++)
            //    {
            //        var itemIds = message.CartItems.Skip(i).Take(_batchSize).Select(x => new CartItemUpdateRequest
            //        {
            //            CartItemId = x.CartItemId,
            //            Currency = x.Currency,
            //            Price = x.Price,
            //        });
            //        var json = new StringContent(
            //                JsonSerializer.Serialize(itemIds, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            //                Encoding.UTF8,
            //                MediaTypeNames.Application.Json);
            //        httpTasks.Add(client.PutAsync($"{_cartServiceUrl}/cart/{message.CartId}/items", json));
            //    }
            //    var httpResponses = await Task.WhenAll(httpTasks);
            //    _logger.LogInformation($"Received a response from API for cart Id: {message.CartId} at {DateTime.Now}");
            //    if (httpResponses != null && httpResponses.All(x=>x.IsSuccessStatusCode))
            //    {
            //        return;
            //    }
            //}
            await Parallel.ForEachAsync(pricingResponses, async (message, _) =>
            {
                message.CartItems.ForEach(x =>
                {
                    cartItemUpdateRequest.Add(new CartItemUpdateRequest
                    {
                        CartItemId = x.CartItemId,
                        Currency = x.Currency,
                        Price = x.Price
                    });
                });
                using (HttpClient client = _httpClientFactory.CreateClient())
                {
                    var json = new StringContent(
                            JsonSerializer.Serialize(cartItemUpdateRequest, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
                            Encoding.UTF8,
                            MediaTypeNames.Application.Json);
                    var httpResponse = await client.PutAsync($"{_cartServiceUrl}/cart/{message.CartId}/items", json);
                    if (httpResponse != null && httpResponse.IsSuccessStatusCode)
                    {
                        await UpdateCart(message);
                        return;
                    }
                }
            });
        }

        private async Task UpdateCart(PricingResponse pricingResponse)
        {
            var cartUpdate = new CartUpdateRequest
            {
                CartId = pricingResponse.CartId,
                PriceListId = pricingResponse.PriceListId,
                StatusId = CartStatus.Priced,
                Price = pricingResponse.TotalPrice
            };
           
            using (var client2 = _httpClientFactory.CreateClient())
            {
                var cartJson = JsonSerializer.Serialize(cartUpdate, new JsonSerializerOptions(JsonSerializerDefaults.Web));
                var content = new StringContent(cartJson, Encoding.UTF8, MediaTypeNames.Application.Json);
                var updateCartResponse = await client2.PutAsync($"{_cartServiceUrl}/cart/{pricingResponse.CartId}", content);
                if (updateCartResponse != null && updateCartResponse.IsSuccessStatusCode)
                    return;
            }
        }

    }
}
