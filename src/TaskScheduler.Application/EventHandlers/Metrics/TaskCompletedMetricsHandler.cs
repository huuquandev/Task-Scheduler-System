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
    public class TaskCompletedMetricsHandler : INotificationHandler<DomainEventNotification<TaskCompletedEvent>>
    {
            private readonly IMetricsService _metrics; 
            public TaskCompletedMetricsHandler(IMetricsService metrics) => _metrics = metrics;
            public Task Handle(DomainEventNotification<TaskCompletedEvent> notification, CancellationToken cancellationToken) => _metrics.IncrementCompletedTasksAsync();
    }
}