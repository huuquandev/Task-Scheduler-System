using System;
using System.Collections.Generic;
using MediatR;
using Microsoft.Extensions.Logging;
using TaskScheduler.Application.Common.EventNotifications;
using TaskScheduler.Domain.Events;
namespace TaskScheduler.Application.EventHandlers.Logging
{
    public class TaskFailedLogHandler : INotificationHandler<DomainEventNotification<TaskFailedEvent>>
    {
        private readonly ILogger<TaskFailedLogHandler> _logger;

        public TaskFailedLogHandler(ILogger<TaskFailedLogHandler> logger) => _logger = logger;

        public Task Handle(DomainEventNotification<TaskFailedEvent> notification, CancellationToken cancellationToken)
        {
            _logger.LogError("Task failed: {TaskId}. Reason: {Reason}", notification.DomainEvent.TaskId, notification.DomainEvent.Reason);
            
            return Task.CompletedTask;
        }
    }
}