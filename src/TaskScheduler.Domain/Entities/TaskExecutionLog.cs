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

        public int? ExitCode { get; private set; }

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

        public void MarkAsSuccess()
        {
            if (Status != TaskExecutionStatus.Running)
            {
                throw new InvalidOperationException("Only running task can be completed.");
            }

            FinishedAt = DateTime.UtcNow;

            Status = TaskExecutionStatus.Success;

            DurationMs = CalculateDurationMs();
        }

        public void MarkAsFailed(string errorMessage)
        {
            if (Status != TaskExecutionStatus.Running)
            {
                throw new InvalidOperationException("Only running task can fail.");
            }

            if (string.IsNullOrWhiteSpace(errorMessage))
            {
                throw new ArgumentException("Error message cannot be empty.");
            }

            FinishedAt = DateTime.UtcNow;

            Status = TaskExecutionStatus.Failed;

            ErrorMessage = errorMessage;

            DurationMs = CalculateDurationMs();
        }

        public void Retry()
        {
            FinishedAt = null;

            ErrorMessage = null;

            DurationMs = null;

            StartedAt = DateTime.UtcNow;

            Status = TaskExecutionStatus.Running;
        }

        private long CalculateDurationMs()
        {
            return (long)(DateTime.UtcNow - StartedAt).TotalMilliseconds;
        }

        public void SetExecutionDetails(string? errorMessage, long? durationMs, int? exitCode)
        {
            ErrorMessage = errorMessage;
            DurationMs = durationMs;
            ExitCode = exitCode;
        }
    }
}