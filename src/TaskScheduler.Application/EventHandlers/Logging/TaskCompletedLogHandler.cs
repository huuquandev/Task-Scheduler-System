using System;
using System.Collections.Generic;
using MediatR;
using Microsoft.Extensions.Logging;
using TaskScheduler.Application.Common.EventNotifications;
using TaskScheduler.Domain.Events;
namespace TaskScheduler.Application.EventHandlers.Logging
{
    public class TaskCompletedLogHandler: INotificationHandler<DomainEventNotification<TaskCompletedEvent>>
    {
        private readonly ILogger<TaskCompletedLogHandler> _logger;

        public TaskCompletedLogHandler(ILogger<TaskCompletedLogHandler> logger) => _logger = logger;

        public Task Handle(DomainEventNotification<TaskCompletedEvent> notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Task completed: {TaskId}", notification.DomainEvent.TaskId);

            return Task.CompletedTask;
        }
    }
}