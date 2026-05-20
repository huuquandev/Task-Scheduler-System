using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskScheduler.Application.Interfaces;
using TaskScheduler.Domain.Entities;
using TaskScheduler.Infrastructure.Persistence;
namespace TaskScheduler.Infrastructure.Scheduling
{
    public class TaskExecutionService : ITaskExecutionService
    {
        private readonly ITaskRepository _repo;
        private readonly ITaskExecutionLogRepository _logrepo;
        private readonly ApplicationDbContext _context;  
        public TaskExecutionService(ITaskRepository repo, ITaskExecutionLogRepository logrepo, ApplicationDbContext context)
        {
            _repo = repo;
            _logrepo = logrepo;
            _context = context;
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
            await _context.SaveChangesAsync();
        }

        private Task ExecuteCommand(ScheduledTask task)
        {
            Console.WriteLine($"Executing task: {task.Id}");

            return Task.CompletedTask;
        }
    }
}