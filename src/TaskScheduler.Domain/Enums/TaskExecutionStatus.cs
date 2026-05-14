using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TaskScheduler.Domain.Enums
{
    public enum TaskExecutionStatus
    {
        Running = 0,

        Success = 1,

        Failed = 2
    }
}