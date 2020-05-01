using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace BlazingReceipts.ReceiptWorker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args)
        {
            return WebHost.CreateDefaultBuilder(args)
                .UseConfiguration(new ConfigurationBuilder()
                    .AddJsonFile($"appsettings.Development.json", optional: false)
                    .AddEnvironmentVariables()
                    .AddCommandLine(args)
                    .Build())
                .UseStartup<Startup>()
                .Build();
        }
    }
}
