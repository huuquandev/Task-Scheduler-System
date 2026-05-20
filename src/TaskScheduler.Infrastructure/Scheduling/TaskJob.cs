using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskScheduler.Application.Interfaces;

namespace TaskScheduler.Infrastructure.Scheduling
{
    public class TaskJob
    {
        private readonly ITaskExecutionService _executionService;

        public TaskJob(ITaskExecutionService executionService)
        {
            _executionService = executionService;
        }

        public Task Execute(Guid taskId)
        {
            return _executionService.ExecuteTask(taskId);
        }
    }
}