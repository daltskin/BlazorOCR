using BlazingReceipts.Client.Services;
using Microsoft.AspNetCore.Blazor.Hosting;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace BlazingReceipts.Client
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);

            builder.Services.AddSingleton<BlazingReceipts.Shared.IReceiptService, ReceiptService>();
            builder.Services.AddTransient<HubConnectionBuilder>();

            builder.RootComponents.Add<App>("app");
            await builder.Build().RunAsync();
        }
    }
}
