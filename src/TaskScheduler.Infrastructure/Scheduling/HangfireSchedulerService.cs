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
        public Task ScheduleTaskAsync(ScheduledTask task)
        {
             RecurringJob.AddOrUpdate<TaskJob>(
                task.Id.ToString(),
                job => job.Execute(task.Id),
                task.CronExpression.Value
            );

            return Task.CompletedTask;
        }

        public Task UnscheduleTaskAsync(Guid taskId)
        {
            RecurringJob.RemoveIfExists(taskId.ToString());

            return Task.CompletedTask;
        }

        public Task RescheduleTaskAsync(ScheduledTask task)
        {
            RecurringJob.AddOrUpdate<TaskJob>(
                task.Id.ToString(),
                job => job.Execute(task.Id),
                task.CronExpression.Value
            );

            return Task.CompletedTask;
        }
    }
}