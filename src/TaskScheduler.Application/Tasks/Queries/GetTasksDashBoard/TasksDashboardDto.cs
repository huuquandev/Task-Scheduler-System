using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TaskScheduler.Application.Tasks.Queries.GetTasksDashBoard
{
    public class TasksDashboardDto
    {
        public int TotalTasks { get; set; }

        public int ActiveTasks { get; set; }

        public int PausedTasks { get; set; }

        public int DeletedTasks { get; set; }

        public int RunningTasks { get; set; }

        public int FailedExecutions { get; set; }

        public int SuccessExecutions { get; set; }
    }
}