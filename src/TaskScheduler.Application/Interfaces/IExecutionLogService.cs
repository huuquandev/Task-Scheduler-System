using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TaskScheduler.Application.Interfaces
{
    public interface IExecutionLogService
    {
        Task LogTaskCompletedAsync(Guid taskId);

        Task LogTaskFailedAsync(Guid taskId, string errorMessage);

        Task LogTaskPausedAsync(Guid taskId);

        Task LogTaskResumedAsync(Guid taskId);
    }
}