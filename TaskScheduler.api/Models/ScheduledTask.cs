using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TaskScheduler.api.Models
{
    public class ScheduledTask
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string CronExpression { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";
        public int RetryCount { get; set; } = 0;
        public int TimeoutSeconds { get; set; } = 60;
        public DateTime? NextRunAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Guid UserId { get; set; }
        public User? User { get; set; }
    }
}