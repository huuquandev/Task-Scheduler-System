using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hangfire.Dashboard;

namespace TaskScheduler.Api.Authorization
{
    public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();

            var ip = httpContext.Connection.RemoteIpAddress;

            return ip?.ToString() == "127.0.0.1" || ip?.ToString() == "::1";
        }
    }
}