using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using TaskScheduler.Application.Common.EventNotifications;
using TaskScheduler.Domain.Events;
namespace TaskScheduler.Application.EventHandlers.Logging
{
    public class TaskCompletedLogHandler: INotificationHandler<DomainEventNotification<TaskCompletedEvent>>
    {
        public async Task Handle(DomainEventNotification<TaskCompletedEvent> notification, CancellationToken cancellationToken)
        {
            var domainEvent = notification.DomainEvent;

            Console.WriteLine($"Task completed: {domainEvent.TaskId}");
        }
    }
}