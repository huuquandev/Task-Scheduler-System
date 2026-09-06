using Microsoft.Extensions.Logging;
using TaskScheduler.Application.Interfaces;

namespace TaskScheduler.Infrastructure.Services
{
    public class ExecutionLogService : IExecutionLogService
    {
        private readonly ILogger<ExecutionLogService> _logger;
        public ExecutionLogService(ILogger<ExecutionLogService> logger) => _logger = logger;

        public Task LogTaskCompletedAsync(Guid taskId)
        {
            _logger.LogInformation("Task {TaskId} completed", taskId);
            return Task.CompletedTask;
        }

        public Task LogTaskFailedAsync(Guid taskId, string errorMessage)
        {
            _logger.LogError("Task {TaskId} failed: {Error}", taskId, errorMessage);
            return Task.CompletedTask;
        }

        public Task LogTaskPausedAsync(Guid taskId)
        {
            _logger.LogInformation("Task {TaskId} paused", taskId);
            return Task.CompletedTask;
        }

        public Task LogTaskResumedAsync(Guid taskId)
        {
            _logger.LogInformation("Task {TaskId} resumed", taskId);
            return Task.CompletedTask;
        }
    }
}
