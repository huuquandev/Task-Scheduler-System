using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TaskScheduler.Domain.Enums
{
    public enum ScheduledTaskStatus
    {
        Active = 1,
        Running = 2,
        Paused = 3,
        Failed = 4,
        Completed = 5
    }
}