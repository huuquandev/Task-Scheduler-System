using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskScheduler.Domain.Common;
using TaskScheduler.Domain.Enums;
using TaskScheduler.Domain.Events;
using TaskScheduler.Domain.ValueObjects;

namespace TaskScheduler.Domain.Entities
{
    public class ScheduledTask : BaseEntity
    {
        public ICollection<TaskExecutionLog> ExecutionLogs { get; private set; } = new List<TaskExecutionLog>();
        
        public Guid Id { get; private set; } //Id

        public string Name { get; private set; } // Name task

        public string Description { get; private set; } // Des task

        public CronExpression CronExpression{ get; private set; } // The cron expression defines the execution schedule.

        public string Command { get; private set; } // The command or action task will be executed.

        public ScheduledTaskStatus Status { get; private set; } // The current status of the task.

        public DateTime? LastRunAt { get; private set; } // The time when the task was last run.

        public DateTime? NextRunAt { get; private set; } // The time when the task will run next.

        public int RetryCount { get; private set; } // The number of times the task has failed and retried.

        public int MaxRetries { get; private set; } // The maximum number of times the task can be retried.
        public bool IsDeleted { get; private set; } // Soft delete flag.

        public DateTime CreatedAt { get; private set; } // The time when the task was created.

        public DateTime UpdatedAt { get; private set; } // The time when the task was last updated.
        private ScheduledTask()
        {
            // EF Core cần
        }

        public ScheduledTask(string name, string description, string cronExpression, string command, int maxRetries)
        {
            Id = Guid.NewGuid();

            Name = name;
            Description = description;
            CronExpression = CronExpression.Create(cronExpression);
            Command = command;

            MaxRetries = maxRetries;

            Status = ScheduledTaskStatus.Pending;

            RetryCount = 0;

            IsDeleted = false;

            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            AddDomainEvent(new TaskCreatedEvent(Id, Name));
        }
        public void MarkAsActive()
        {
            Status = ScheduledTaskStatus.Active;
            UpdatedAt = DateTime.UtcNow;
        }
        public void MarkAsRunning()
        {
            Status = ScheduledTaskStatus.Running;

            LastRunAt = DateTime.UtcNow;

            UpdatedAt = DateTime.UtcNow;
        }
        public void Pause()
        {
            if (Status != ScheduledTaskStatus.Active)
            {
                throw new InvalidOperationException("Only active task can be paused.");
            }

            Status = ScheduledTaskStatus.Paused;
            AddDomainEvent(new TaskPausedEvent(Id));

            UpdatedAt = DateTime.UtcNow;
        }
        public void MarkAsFailed(string reason)
        {
            Status = ScheduledTaskStatus.Failed;

            RetryCount++;

            UpdatedAt = DateTime.UtcNow;
            AddDomainEvent(new TaskFailedEvent(Id, reason));
        }
        public void MarkAsCompleted()
        {
            Status = ScheduledTaskStatus.Completed;
            AddDomainEvent(new TaskCompletedEvent(Id));
        }
        public void UpdateNextRunTime()
        {
            var cron = Cronos.CronExpression.Parse(CronExpression.Value);

            NextRunAt = cron.GetNextOccurrence(DateTime.UtcNow);
        }
        public void SoftDelete()
        {
            IsDeleted = true;

            UpdatedAt = DateTime.UtcNow;
        }

        public void Update(string name, string description, CronExpression cronExpression, string command, int maxRetries)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name invalid");
                
            if (maxRetries < 0)
                throw new ArgumentException("MaxRetries invalid");

            Name = name;
            Description = description;
            CronExpression = cronExpression;
            Command = command;
            MaxRetries = maxRetries;

            UpdatedAt = DateTime.UtcNow;
        }

        public void IncreaseRetryCount()
        {
            RetryCount++;
        }
        public void ResetRetryCount()
        {
            RetryCount = 0;
        }
    }
}