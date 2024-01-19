using Cart_Worker.HostedService;
using Cart_Worker.MessageHandler;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
namespace Cart_worker
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
              services.AddHttpClient();
              services.AddHostedService<CartHostedService>();
              services.AddTransient<IMessageHandler, CartMessageHandler>();

          });
    }
}