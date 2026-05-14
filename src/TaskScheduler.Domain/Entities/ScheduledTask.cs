using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TaskScheduler.Domain.Entities
{
    public class ScheduledTask
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string CronExpression { get; set; } = string.Empty;

        public string Status { get; set; } = "Active";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}