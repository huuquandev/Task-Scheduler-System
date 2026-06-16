using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using System.Diagnostics;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using TaskScheduler.Application.Common.Telemetry;
using TaskScheduler.Application.Interfaces;
using Microsoft.AspNetCore.Http;
namespace TaskScheduler.Application.Common.Behaviors
{
    public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
    {
        private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger, IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            var requestName = typeof(TRequest).Name;
            var correlationId = _httpContextAccessor.HttpContext?.Items["CorrelationId"]?.ToString();
            var requestBody = request switch
            {
                ISensitiveRequest => "[REDACTED]",
                _ => JsonSerializer.Serialize(request)
            };

            using var activity = TelemetryConfig.ActivitySource.StartActivity(requestName, ActivityKind.Internal);

            activity?.SetTag("request.name", requestName);
            activity?.SetTag("request.body", requestBody);
            activity?.SetTag("correlation.id", correlationId);
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("Handling request {RequestName}. Payload: {Payload}. Correlation ID: {CorrelationId}", requestName, requestBody, correlationId);

                var response = await next();

                stopwatch.Stop();

                activity?.SetTag("request.status", "Success");
                activity?.SetStatus(ActivityStatusCode.Ok);

                _logger.LogInformation("Handled request {RequestName} in {ElapsedMilliseconds}ms. Correlation ID: {CorrelationId}", requestName, stopwatch.ElapsedMilliseconds, correlationId);
                // Slow Request Warning
                if (stopwatch.ElapsedMilliseconds > 1000)
                {
                    _logger.LogWarning("Slow request detected: {RequestName} took {ElapsedMilliseconds}ms. Correlation ID: {CorrelationId}", requestName, stopwatch.ElapsedMilliseconds, correlationId);
                }
                return response;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                activity?.SetTag("error.message", ex.Message);

                activity?.SetTag("error.type", ex.GetType().Name);

                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

                _logger.LogError(ex, "Error handling request {RequestName} after {ElapsedMilliseconds}ms. Correlation ID: {CorrelationId}", requestName, stopwatch.ElapsedMilliseconds, correlationId);

                throw;
            }
        }
    }
}