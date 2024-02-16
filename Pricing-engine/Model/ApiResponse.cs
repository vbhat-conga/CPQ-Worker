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
        public ApiResponse(T data, int statusCode)
        {
            Data = data;
            StatusCode = statusCode;
        }

        public T Data { get; set; }
        public int StatusCode { get; set; }

    }
}
