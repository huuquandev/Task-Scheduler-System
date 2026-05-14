using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskScheduler.Domain.Enums;

namespace TaskScheduler.Domain.Entities
{
    public class TaskExecutionLog
    {
        public Guid Id { get; private set; }

        public Guid TaskId { get; private set; }

        public ScheduledTask ScheduledTask { get; private set; }

        public DateTime StartedAt { get; private set; }

        public DateTime? FinishedAt { get; private set; }

        public TaskExecutionStatus Status { get; private set; }

        public string? ErrorMessage { get; private set; }

        public long? DurationMs { get; private set; }

        private TaskExecutionLog()
        {
        }

        public TaskExecutionLog(Guid taskId)
        {
            Id = Guid.NewGuid();

            TaskId = taskId;

            StartedAt = DateTime.UtcNow;

            Status = TaskExecutionStatus.Running;
        }
    }
}