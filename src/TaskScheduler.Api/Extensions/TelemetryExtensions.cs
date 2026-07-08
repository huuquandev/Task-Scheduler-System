using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using TaskScheduler.Application.Common.Telemetry;
namespace TaskScheduler.Api.Extensions
{
    public static class TelemetryExtensions
    {
        public static IServiceCollection AddTelemetry(this IServiceCollection services)
        {
            services.AddOpenTelemetry().WithTracing(tracing =>
            {
                tracing.SetResourceBuilder(ResourceBuilder.CreateDefault()
                       .AddService(TelemetryConfig.ServiceName))
                       .AddSource(TelemetryConfig.ServiceName)
                       .AddAspNetCoreInstrumentation()
                       .AddHttpClientInstrumentation()
                       .AddConsoleExporter();
            });

            return services;
        }
    }
}