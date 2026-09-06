using Microsoft.Extensions.Logging;
using TaskScheduler.Application.Common.Models;
using TaskScheduler.Application.Interfaces;

namespace TaskScheduler.Infrastructure.Services
{
    public class NoOpSmsService : ISmsService
    {
        private readonly ILogger<NoOpSmsService> _logger;
        public NoOpSmsService(ILogger<NoOpSmsService> logger) => _logger = logger;

        public Task SendSmsAsync(SmsMessage message)
        {
            _logger.LogInformation("SMS to {Phone}: {Message}", message.PhoneNumber, message.Message);
            return Task.CompletedTask;
        }
    }
}
