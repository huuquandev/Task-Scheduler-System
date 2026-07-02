using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using TaskScheduler.Application.Interfaces;
using TaskScheduler.Domain.Entities;
namespace TaskScheduler.Infrastructure.Scheduling
{
    public class HangfireSchedulerService : ISchedulerService
    {
        private readonly IRecurringJobManager _recurringJobManager;

        public HangfireSchedulerService(IRecurringJobManager recurringJobManager)
        {
            _recurringJobManager = recurringJobManager;
        }
        public Task ScheduleTaskAsync(ScheduledTask task)
        {
             _recurringJobManager.AddOrUpdate<TaskJob>(
                task.Id.ToString(),
                job => job.Execute(task.Id),
                task.CronExpression.Value
            );

            return Task.CompletedTask;
        }

        public Task UnscheduleTaskAsync(Guid taskId)
        {
            _recurringJobManager.RemoveIfExists(taskId.ToString());

            return Task.CompletedTask;
        }

        public Task RescheduleTaskAsync(ScheduledTask task)
        => ScheduleTaskAsync(task);
    }
}