using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Text.Json;
using TaskScheduler.Application.Common.Models;

namespace TaskScheduler.Api.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);

                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var response = exception switch
            {
                // Map exception type → HTTP status
                ArgumentException => ApiResponse<object>.FailureResponse(
                    exception.Message, 
                    StatusCodes.Status400BadRequest),
                
                InvalidOperationException => ApiResponse<object>.FailureResponse(
                    exception.Message, 
                    StatusCodes.Status400BadRequest),
                
                KeyNotFoundException => ApiResponse<object>.FailureResponse(
                    exception.Message, 
                    StatusCodes.Status404NotFound),
                
                // Default: 500 Internal Server Error
                _ => ApiResponse<object>.FailureResponse(
                    "An internal server error occurred", 
                    StatusCodes.Status500InternalServerError)
            };

            context.Response.StatusCode = response.Code;

            await context.Response.WriteAsJsonAsync(response);
        }
    }
}