using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
namespace TaskScheduler.Api.Extensions
{
    public static class RateLimitExtensions
    {
        public static IServiceCollection AddRateLimiting(this IServiceCollection services)
        {
            services.AddRateLimiter(options =>
            {
                options.AddFixedWindowLimiter(
                    policyName: "trigger-policy",
                    configureOptions =>
                    {
                        configureOptions.PermitLimit = 10;

                        configureOptions.Window = TimeSpan.FromMinutes(1);

                        configureOptions.QueueLimit = 0;

                        configureOptions.QueueProcessingOrder =
                            QueueProcessingOrder.OldestFirst;
                    });

                options.RejectionStatusCode = 429;
            });

            return services;
        }
    }
}