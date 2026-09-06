using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using TaskScheduler.Application.Common.EventNotifications;
using TaskScheduler.Domain.Events;
using TaskScheduler.Application.Interfaces;

namespace TaskScheduler.Application.EventHandlers.Metrics
{
    public class TaskFailedMetricsHandler : INotificationHandler<DomainEventNotification<TaskFailedEvent>>
    {
            private readonly IMetricsService _metrics;
            public TaskFailedMetricsHandler(IMetricsService metrics) => _metrics = metrics;
            public Task Handle(DomainEventNotification<TaskFailedEvent> notification, CancellationToken cancellationToken) => _metrics.IncrementFailedTasksAsync();
    }
}