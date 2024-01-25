using Config_engine.Worker.Messagehandler;
using Config_engine.Worker.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Config_engine.Worker.HostedService
{
    internal class ConfigHostedService : BackgroundService
    {
        private readonly ILogger<ConfigHostedService> _logger;
        private readonly IDatabase _database;
        private readonly string _consumerGroupName;
        private readonly string _redisStreamName;
        private readonly IMessageHandler _messageHandler;
        private readonly IConfiguration _configuration;
        private readonly IConnectionMultiplexer _connectionMultiplexer;
        
        public ConfigHostedService(ILogger<ConfigHostedService> logger, IMessageHandler messageHandler, IConfiguration configuration, IConnectionMultiplexer connectionMultiplexer)
        {
            _logger = logger;
            _configuration = configuration;            
            _redisStreamName = _configuration.GetValue<string>("ConfigStream") ?? "config-stream";
            _consumerGroupName = _configuration.GetValue<string>("ConsumerGroup") ?? "config-engine";
            _messageHandler = messageHandler;
            _connectionMultiplexer = connectionMultiplexer;
            _database = _connectionMultiplexer.GetDatabase();
        }

        // TODO: Proper exception handling
        // What happens if it fails to do its job?
        // Code refactoring and duplicate code removal etc.
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                if (!await _database.KeyExistsAsync(_redisStreamName) || !(await _database.StreamGroupInfoAsync(_redisStreamName)).Any(x => x.Name.Equals(_consumerGroupName, StringComparison.InvariantCultureIgnoreCase)))
                {
                    _logger.LogInformation($"creating consumer group: {_consumerGroupName} or stream : {_redisStreamName}");
                    await _database.StreamCreateConsumerGroupAsync(_redisStreamName, _consumerGroupName, "0-0", createStream: true);
                }
            }
            catch(RedisException ex)
            {
                _logger.LogError(ex.Message, "Error while creating consumer group", ex);
            }
            var consumerName = $"{_consumerGroupName}-{Guid.NewGuid()}";
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var streamEntries = await _database.StreamReadGroupAsync(_redisStreamName, _consumerGroupName, consumerName, ">", count: 1);
                    if (streamEntries != null && streamEntries.Length > 0)
                    {
                        _logger.LogInformation($"Received a new message at {DateTime.Now}");
                        var dict = streamEntries.ToDictionary(x => x.Id.ToString(), x => ConvertToObject<CartMessage>(x.Values));
                        var cartMessage = dict.Select(x => x.Value).ToList();
                        var messageIds = dict.Select(x => new RedisValue(x.Key)).ToArray();
                        if (cartMessage != null && cartMessage.Count > 0)
                        {
                            await _messageHandler.HandleMessage(cartMessage, _database);
                            _logger.LogInformation($"Complete processing a message at {DateTime.Now}");
                            await _database.StreamAcknowledgeAsync(_redisStreamName, _consumerGroupName, messageIds);
                            _logger.LogInformation($"Acknowledgement sent to redis for message at {DateTime.Now}");
                            await _database.StreamDeleteAsync(_redisStreamName, messageIds);
                            _logger.LogInformation($"Request for deleting the message sent at: {DateTime.Now}");
                        }
                    }
                }
                catch(Exception ex)
                {
                    _logger.LogError(ex.Message, "Error while processing message in config-engine", ex);
                }
            }
        }

        private T ConvertToObject<T>(NameValueEntry[] values)
        {
            var result = default(T);
            foreach (var entry in values)
            {
                if (!entry.Value.IsNull)
                {
                   result = JsonSerializer.Deserialize<T>(entry.Value);
                }
            }
            return result;
        }
    }
}
