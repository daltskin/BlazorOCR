using BlazingReceipts.ReceiptWorker.Hubs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polly;
using Polly.Extensions.Http;
using System;
using System.Threading.Tasks;

namespace BlazingReceipts.ReceiptWorker
{
    public class Startup
    {
        private readonly string MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy(MyAllowSpecificOrigins,
                builder =>
                {
                    builder.AllowAnyHeader()
                        .AllowAnyMethod()
                        .SetIsOriginAllowed((host) => true)
                        .AllowCredentials();
                });
            });

            services.AddSignalR()
                .AddJsonProtocol(options =>
                {
                    options.PayloadSerializerOptions.PropertyNamingPolicy = null;
                });

            var policy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(response => response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                .WaitAndRetryAsync(retryCount: 3,
                    sleepDurationProvider: (retryCount, response, context) =>
                    {
                        TimeSpan delay = response?.Result?.Headers?.RetryAfter == null ? TimeSpan.Zero : response.Result.Headers.RetryAfter.Delta.Value;
                        Console.WriteLine($"Throttled! - honour retry duration {delay.TotalSeconds} secs");
                        return delay;
                    },
                    onRetryAsync: (response, timesSpan, retryCount, context) => Task.CompletedTask
                   );

            services.AddHttpClient<Worker>("cs")
                .AddPolicyHandler(policy);

            services.AddHostedService<Worker>();
        }

        public void Configure(IApplicationBuilder app, IHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseCors(MyAllowSpecificOrigins);
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<OCRStatusHub>("/Hubs/OCRStatusHub");
            });
        }
    }
}
