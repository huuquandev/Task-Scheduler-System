using MediatR;
using Microsoft.Extensions.Configuration;
using TaskScheduler.Application.Common.EventNotifications;
using TaskScheduler.Application.Common.Models;
using TaskScheduler.Application.Interfaces;
using TaskScheduler.Domain.Events;

namespace TaskScheduler.Application.EventHandlers.Notifications
{
    public class SendEmailHandler : INotificationHandler<DomainEventNotification<TaskFailedEvent>>
    {
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;

        public SendEmailHandler(IEmailService emailService, IConfiguration configuration)
        {
            _emailService = emailService;
            _configuration = configuration;
        }

        public Task Handle(DomainEventNotification<TaskFailedEvent> notification, CancellationToken cancellationToken)
        {
            var ev = notification.DomainEvent;
            var notifyEmail = _configuration["Notifications:AdminEmail"];
            if (string.IsNullOrWhiteSpace(notifyEmail))
                return Task.CompletedTask;

            var message = new EmailMessage
            {
                To = notifyEmail,
                Subject = $"Task {ev.TaskId} failed",
                Body = $"Task {ev.TaskId} failed. Reason: {ev.Reason}"
            };
            return _emailService.SendEmailAsync(message);
        }
    }
}