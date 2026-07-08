using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Hangfire;
using Hangfire.Dashboard;
using TaskScheduler.Api.Authorization;
namespace TaskScheduler.Api.Extensions
{
    public static class HangfireExtensions
    {
        public static IApplicationBuilder UseHangfireDashboardConfiguration(this IApplicationBuilder app, IHostEnvironment environment)
        {
            if (!environment.IsEnvironment("Testing"))
            {
                app.UseHangfireDashboard("/hangfire", new DashboardOptions
                {
                    Authorization =
                    [
                        new HangfireAuthorizationFilter()
                    ]
                });
            }

            return app;
        }
    }
}