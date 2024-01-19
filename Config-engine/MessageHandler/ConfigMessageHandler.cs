using Config_engine.Worker.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Config_engine.Worker.Messagehandler
{
    internal class ConfigMessageHandler : IMessageHandler
    {
        private readonly int _batchSize;
        private readonly ILogger<ConfigMessageHandler> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly string _pricingStream;
        private readonly string _adminServiceUrl;
        public ConfigMessageHandler(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<ConfigMessageHandler> logger)
        {
            _configuration = configuration;
            _batchSize = _configuration.GetValue<int>("BatchSize");
            _httpClientFactory = httpClientFactory;            
            _pricingStream = _configuration.GetValue<string>("PricingStream") ?? "pricing-stream";
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
            //    using HttpClient client = _httpClientFactory.CreateClient();
            //    for (var i = 0; i < iteration; i++)
            //    {
            //        var itemIds = cartItems.Skip(i).Take(_batchSize).Select(x => x.Product.Id.ToString());

            //        var json = new StringContent(
            //                JsonSerializer.Serialize(itemIds, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            //                Encoding.UTF8,
            //                MediaTypeNames.Application.Json);
            //        httpTasks.Add(client.PostAsync($"{_adminServiceUrl}//product/query, json));
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
            //            var responseString = await httpResponse.Content.ReadAsStringAsync();
            //            var response = JsonSerializer.Deserialize<ApiResponse<List<ProductData>>>(responseString, options)!;
            //            if (response != null)
            //            {
            //                response.Data.ForEach(x => { productList.Add(x); });
            //            }
            //        }
            //    }

            //    ApplyRules(productList, message);
            //    await SendToPricing(message, database);
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
                    var itemIds = cartItems.Skip(i).Take(_batchSize).Select(x => x.Product.Id.ToString());
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
                _logger.LogInformation($"Total products {productList.Count} has been retrieved and deserialized at: {DateTime.Now}");
                ApplyRules(productList, message);
                await SendToPricing(message, database);
            });
        }

        private async Task SendToPricing(CartMessage message, IDatabase database)
        {
            var nameValueEntry = new NameValueEntry[]
            {
                new NameValueEntry(nameof(message),JsonSerializer.Serialize(message))
            };
          
            await database.StreamAddAsync(_pricingStream, nameValueEntry);
            _logger.LogInformation($"message for cart id : {message.CartId} has been sent to pricing-engine at: {DateTime.Now}");
        }

        private void ApplyRules(ConcurrentBag<ProductData> productList, CartMessage message)
        {
            //check all stand alone product wihtou any rule and nothing to configure.
            if (IsStandAlone(productList))
            {

            }
        }

        private bool IsStandAlone(ConcurrentBag<ProductData> productList)
        {
            return productList.All(x => x.IsPlainProduct);
        }
    }
}
