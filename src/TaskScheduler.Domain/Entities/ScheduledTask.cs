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
        public ICollection<TaskExecutionLog> ExecutionLogs
        {
            get;
            private set;
        } = new List<TaskExecutionLog>();
        
        public Guid Id { get; private set; } //Id

        public string Name { get; private set; } // Name task

        public string Description { get; private set; } // Des task

        public CronExpression CronExpression{ get; private set; } // Biểu thức cron xác định lịch chạy.

        public string Command { get; private set; } // Lệnh hoặc action task sẽ thực thi.

        public ScheduledTaskStatus Status { get; private set; } // Trạng thái hiện tại của task.

        public DateTime? LastRunAt { get; private set; } // Task chạy lần gần nhất.

        public DateTime? NextRunAt { get; private set; } // Task chạy lần tiếp theo.

        public int RetryCount { get; private set; } // Số lần chạy lại task.

        public int MaxRetries { get; private set; } // Giới hạn lần chạy lại tối đa.

        public bool IsDeleted { get; private set; } // Soft delete flag.

        public DateTime CreatedAt { get; private set; } // Ngày tạo task

        public DateTime UpdatedAt { get; private set; } // Ngày lần cuối update task
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
        public void Activate()
        {
            Status = ScheduledTaskStatus.Active;

            UpdatedAt = DateTime.UtcNow;
        }
        public void StartRunning()
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

            UpdatedAt = DateTime.UtcNow;
        }
        public void MarkAsFailed(string reason)
        {
            Status = ScheduledTaskStatus.Failed;

            RetryCount++;

            UpdatedAt = DateTime.UtcNow;
            AddDomainEvent(new TaskFailedEvent(Id, reason));
        }
        public void Complete()
        {
            Status = ScheduledTaskStatus.Completed;
        }
         public void SetNextRun(DateTime nextRunAt)
        {
            NextRunAt = nextRunAt;

            UpdatedAt = DateTime.UtcNow;
            AddDomainEvent(new TaskCompletedEvent(Id));
        }

        public void SoftDelete()
        {
            IsDeleted = true;

            UpdatedAt = DateTime.UtcNow;
        }

        public bool CanRetry()
        {
            return RetryCount < MaxRetries;
        }
    }
}