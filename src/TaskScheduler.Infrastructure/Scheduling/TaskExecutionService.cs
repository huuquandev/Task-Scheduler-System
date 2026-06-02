using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using TaskScheduler.Application.Interfaces;
using TaskScheduler.Domain.Entities;
namespace TaskScheduler.Infrastructure.Scheduling
{
    public class TaskExecutionService : ITaskExecutionService
    {
        private readonly ITaskRepository _repo;
        private readonly ITaskExecutionLogRepository _logrepo;
        private readonly IUnitOfWork _unitOfWork;

        public TaskExecutionService(ITaskRepository repo, ITaskExecutionLogRepository logrepo, IUnitOfWork unitOfWork)
        {
            _repo = repo;
            _logrepo = logrepo;
            _unitOfWork = unitOfWork;
        }

        public async Task ExecuteTask(Guid taskId)
        {
            var task = await _repo.GetByIdAsync(taskId);

            if (task == null) return;
            var log = new TaskExecutionLog(taskId);

            try
            {
                // Task start
                task.MarkAsRunning();
                // Update status task
                await _repo.UpdateAsync(task);

                // Create execution log
                await _logrepo.AddAsync(log);

                // Save change to DB
                await _unitOfWork.SaveChangesAsync();

                // Execute task
                await ExecuteCommand(task);

                // SUCCESS
                task.MarkAsActive();
                task.UpdateNextRunTime();
                log.MarkAsSuccess();
            }
            catch (Exception ex)
            {
                // FAILED
                task.MarkAsFailed(ex.Message);

                log.MarkAsFailed(ex.Message);

            }
            await _repo.UpdateAsync(task);
            await _logrepo.AddAsync(log);
            await _unitOfWork.SaveChangesAsync();
        }

        private Task ExecuteCommand(ScheduledTask task)
        {
            Console.WriteLine($"Executing task: {task.Id}");

            return Task.CompletedTask;
        }

        public Task TriggerNow(Guid taskId)
        {
            BackgroundJob.Enqueue<ITaskExecutionService>(
                x => x.ExecuteTask(taskId)
            );
            return Task.CompletedTask;
        }
    }
}