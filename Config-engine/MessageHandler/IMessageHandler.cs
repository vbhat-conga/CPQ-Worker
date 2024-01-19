using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Config_engine.Worker.Model;
using StackExchange.Redis;

namespace Config_engine.Worker.Messagehandler
{
    internal interface IMessageHandler
    {
        Task HandleMessage(List<CartMessage> cartMessage, IDatabase database);
    }
}
