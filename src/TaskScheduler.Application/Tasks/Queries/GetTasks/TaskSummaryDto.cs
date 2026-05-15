using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TaskScheduler.Application.Tasks.Queries.GetTasks
{
    public class TaskSummaryDto
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public string Status { get; set; }
    }
}