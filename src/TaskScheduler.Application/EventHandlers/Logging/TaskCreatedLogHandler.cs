using MediatR;
using Microsoft.Extensions.Logging;
using TaskScheduler.Application.Common.EventNotifications;
using TaskScheduler.Domain.Events;

namespace TaskScheduler.Application.EventHandlers.Logging
{
    public class TaskCreatedLogHandler : INotificationHandler<DomainEventNotification<TaskCreatedEvent>>
    {
        private readonly ILogger<TaskCreatedLogHandler> _logger;
        public TaskCreatedLogHandler(ILogger<TaskCreatedLogHandler> logger) => _logger = logger;

        public Task Handle(DomainEventNotification<TaskCreatedEvent> notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Task created: {TaskId} ({TaskName})", notification.DomainEvent.TaskId, notification.DomainEvent.Name);
            return Task.CompletedTask;
        }
    }
}
