using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Pricing_Engine.Model
{
    public class ApiResponse<T>
    {
        public ApiResponse(T data, string statusCode, string? errorMessage = null)
        {
            Data = data;
            StatusCode = statusCode;
            ErrorMessage = errorMessage;
        }

        public T Data { get; set; }
        public string StatusCode { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ErrorMessage { get; set; }
    }
}
