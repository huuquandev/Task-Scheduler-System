using MediatR;
using TaskScheduler.Application.Common.EventNotifications;
using TaskScheduler.Application.Common.Models;
using TaskScheduler.Application.Interfaces;
using TaskScheduler.Domain.Events;

namespace TaskScheduler.Application.EventHandlers.Notifications
{
    public class SendEmailHandler : INotificationHandler<DomainEventNotification<TaskFailedEvent>>
    {
        private readonly IEmailService _emailService;
        public SendEmailHandler(IEmailService emailService) => _emailService = emailService;

        public Task Handle(DomainEventNotification<TaskFailedEvent> notification, CancellationToken cancellationToken)
        {
            var ev = notification.DomainEvent;
            var message = new EmailMessage
            {
                To = "admin@example.com",   // TODO: lấy từ config hoặc task settings
                Subject = $"Task {ev.TaskId} failed",
                Body = $"Task {ev.TaskId} failed. Reason: {ev.Reason}"
            };
            return _emailService.SendEmailAsync(message);
        }
    }
}