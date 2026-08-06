using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskScheduler.Application.Interfaces;

namespace TaskScheduler.Infrastructure.Services
{
    public class MetricsService : IMetricsService
    {
        public Task IncrementCompletedTasksAsync() => Task.CompletedTask;
        public Task IncrementFailedTasksAsync() => Task.CompletedTask;
    }
}