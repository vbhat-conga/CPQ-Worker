using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pricing_Engine.HostedService;
using Pricing_Engine.MessageHandler;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Exporter;
using OpenTelemetry;
using Pricing_Engine.Model;

namespace Pricing_Engine
{
    internal class Program
    {
        public static IConfigurationRoot Configuration { get; private set; }
        static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args).ConfigureAppConfiguration((hostingContext, config) =>
            {
                config
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile("configoverride/appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();
                Configuration = config.Build();
            })
          .ConfigureLogging((builder) =>
          {
              builder.ClearProviders();
              builder.AddConsole();
          })
          .ConfigureServices((hostingContext, services) =>
          {
              var connection = ConnectionMultiplexer.Connect(hostingContext.Configuration.GetConnectionString("RedisConnectionString")?? "127.0.0.1:6379");
              services.AddSingleton<IConnectionMultiplexer>(connection);
              services.AddHttpClient();
              services.AddHostedService<PricingHostedService>();
              services.AddTransient<IMessageHandler, PricingMessageHandler>();
              Action<ResourceBuilder> configureResource = r => r.AddService(
                 serviceName: hostingContext.Configuration.GetValue("ServiceName", defaultValue: "pricing-engine")!,
                 serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown",
                 serviceInstanceId: Environment.MachineName);

              services.AddOpenTelemetry()
              .WithTracing(builder => builder
                  .ConfigureResource(configureResource)
                  .AddSource(Instrumentation.ActivitySourceName)
                   //.AddConsoleExporter()
                   .AddOtlpExporter(opts =>
                   {
                       opts.Protocol = OtlpExportProtocol.Grpc;
                        opts.Endpoint = new Uri(hostingContext.Configuration.GetValue("Otlp:Endpoint", defaultValue:"http://localhost:4317/api/traces"));
                       opts.ExportProcessorType = ExportProcessorType.Batch;
                   })
                   .AddAspNetCoreInstrumentation()
                   .AddHttpClientInstrumentation());
          });
    }
}