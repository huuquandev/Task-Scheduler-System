using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TaskScheduler.Application.Interfaces
{
    public interface ITaskExecutionService
    {
        /// <summary>
        /// Execute task
        /// </summary>
        Task ExecuteTask(Guid taskId);
    }
}