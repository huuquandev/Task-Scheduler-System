using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskScheduler.Application.Interfaces;
using TaskScheduler.Domain.Entities;

namespace TaskScheduler.Infrastructure.Scheduling
{
    public class TaskExecutionService
    {
        private readonly ITaskRepository _repo;
        private readonly ITaskExecutionLogRepository _logrepo;

        public TaskExecutionService(ITaskRepository repo, ITaskExecutionLogRepository logrepo)
        {
            _repo = repo;
            _logrepo = logrepo;
        }

        public async Task ExecuteTask(Guid taskId)
        {
            var task = await _repo.GetByIdAsync(taskId);
            var log = new TaskExecutionLog(taskId);

            if (task == null) return;

            try
            {
                // Task start
                task.MarkAsRunning();
                await _repo.UpdateAsync(task);

                // Create execution log
                await _logrepo.AddAsync(log);

                // Execute task
                await ExecuteCommand(task);

                // SUCCESS
                task.MarkAsCompleted();
                log.MarkAsSuccess();
            }
            catch (Exception ex)
            {
                // FAILED
                task.MarkAsFailed(ex.Message);

                log.MarkAsFailed(ex.Message);

            }
            await _repo.UpdateAsync(task);
            await _logrepo.UpdateAsync(log);
        }

        private Task ExecuteCommand(ScheduledTask task)
        {
            Console.WriteLine($"Executing task: {task.Id}");

            return Task.CompletedTask;
        }
    }
}