using Pricing_Engine.Model;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pricing_Engine.MessageHandler
{
    internal interface IMessageHandler
    {
        Task HandleMessage(List<CartMessage> cartMessage, IDatabase database);
    }
}
