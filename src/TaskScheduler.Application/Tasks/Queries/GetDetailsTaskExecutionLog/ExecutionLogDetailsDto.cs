using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TaskScheduler.Application.Tasks.Queries.GetDetailsTaskExecutionLog
{
    public class TaskExecutionLogDetailsDto
    {
        public Guid Id { get; set; }
        public Guid TaskId { get; set; }
        public string TaskName { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime FinishedAt { get; set; }
        public string Status { get; set; }
        public string ErrorMessage { get; set; }
        public long? DurationMs { get; set; }
    }
}