using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TaskScheduler.Application.Tasks.Queries.GetTaskExecutionLogs
{
    public class ExecutionLogDto
    {
        public Guid Id { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime FinishedAt { get; set; }
        public string Status { get; set; }
        public string ErrorMessage { get; set; }
    }
}