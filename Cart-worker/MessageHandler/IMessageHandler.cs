using Cart_Worker.Model;
using Cart_Worker;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cart_Worker.MessageHandler
{
    internal interface IMessageHandler
    {
        Task HandleMessage(List<PricingResponse> cartMessage, IDatabase database);
    }
}
