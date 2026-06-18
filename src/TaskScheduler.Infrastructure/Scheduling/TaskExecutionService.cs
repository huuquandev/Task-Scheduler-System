using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using TaskScheduler.Application.Interfaces;
using TaskScheduler.Domain.Entities;
using TaskScheduler.application.Common.Models;
namespace TaskScheduler.Infrastructure.Scheduling
{
    public class TaskExecutionService : ITaskExecutionService
    {
        private readonly ITaskRepository _repo;
        private readonly ITaskExecutionLogRepository _logrepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<TaskExecutionService> _logger;

        public TaskExecutionService(ITaskRepository repo, ITaskExecutionLogRepository logrepo, IUnitOfWork unitOfWork, ILogger<TaskExecutionService> logger)
        {
            _repo = repo;
            _logrepo = logrepo;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task ExecuteTask(Guid taskId)
        {
            var task = await _repo.GetByIdAsync(taskId);

            if (task == null)
            {
                _logger.LogWarning("Task {TaskId} not found", taskId);
                return;
            }

            var log = new TaskExecutionLog(taskId);

            try
            {
                // Task running
                task.MarkAsRunning();

                await _repo.UpdateAsync(task);

                // Create execution log with status = Running
                await _logrepo.AddAsync(log);

                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Starting execution of task {TaskId} ({TaskName})", task.Id, task.Name);

                // Execute command
                var result = await ExecuteCommand(task);

                if (!result.Success)
                {
                    throw new InvalidOperationException( string.IsNullOrWhiteSpace(result.StandardError)? $"Command exited with code {result.ExitCode}" : result.StandardError);
                }
                
                log.MarkAsSuccess();

                task.MarkAsActive();

                task.UpdateNextRunTime();

                _logger.LogInformation("Task {TaskId} completed successfully", task.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Task {TaskId} execution failed", task.Id);
                
                log.MarkAsFailed(ex.Message);

                task.MarkAsFailed(ex.Message);
            }
            finally
            {
                await _repo.UpdateAsync(task);

                await _unitOfWork.SaveChangesAsync();
            }
        }

        public Task TriggerNow(Guid taskId)
        {
            BackgroundJob.Enqueue<ITaskExecutionService>(
                x => x.ExecuteTask(taskId)
            );
            _logger.LogInformation("Enqueue task {TaskId} for immediate execution", taskId);
            return Task.CompletedTask;
        }

        private async Task<CommandExecutionResult> ExecuteCommand(ScheduledTask task)
        {
            const int TimeoutSeconds = 300; // 5 minutes

            var stopwatch = Stopwatch.StartNew();

            using var process = new Process();

            process.StartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",

                Arguments = $"/c {task.Command}",

                RedirectStandardOutput = true,

                RedirectStandardError = true,

                UseShellExecute = false,

                CreateNoWindow = true
            };

            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync();

            var errorTask = process.StandardError.ReadToEndAsync();

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(TimeoutSeconds));

            try
            {
                await process.WaitForExitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill(true);
                    }
                }
                catch
                {
                    // ignore
                }

                throw new TimeoutException($"Command execution exceeded {TimeoutSeconds} seconds.");
            }

            stopwatch.Stop();

            return new CommandExecutionResult
            {
                Success = process.ExitCode == 0,

                ExitCode = process.ExitCode,

                StandardOutput = await outputTask,

                StandardError = await errorTask,

                Duration = stopwatch.Elapsed
            };
        }
    }
}