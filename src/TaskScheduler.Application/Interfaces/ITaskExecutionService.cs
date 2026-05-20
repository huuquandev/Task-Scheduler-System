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
        /// <summary>
        /// Trigger task immediately, regardless of its schedule. This is useful for manual execution or testing purposes.
        /// </summary>
        /// <param name="taskId"></param>
        Task TriggerNow(Guid taskId);

    }
}