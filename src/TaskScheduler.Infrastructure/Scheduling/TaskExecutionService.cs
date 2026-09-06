using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using TaskScheduler.Application.Interfaces;
using TaskScheduler.Domain.Entities;
using TaskScheduler.Application.Common.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
namespace TaskScheduler.Infrastructure.Scheduling
{
    public class TaskExecutionService : ITaskExecutionService
    {
        private readonly ITaskRepository _repo;
        private readonly ITaskExecutionLogRepository _logrepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<TaskExecutionService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IBackgroundJobClient _backgroundJobClient;

        public TaskExecutionService(ITaskRepository repo, ITaskExecutionLogRepository logrepo, IUnitOfWork unitOfWork, ILogger<TaskExecutionService> logger, IConfiguration configuration, IBackgroundJobClient backgroundJobClient)
        {
            _repo = repo;
            _logrepo = logrepo;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _configuration = configuration;
            _backgroundJobClient = backgroundJobClient;
        }

        // Execute the task by its ID, handling the execution flow, logging, and retry logic.
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

                _logger.LogInformation("Starting execution of task {TaskId} ({TaskName}), RetryCount={RetryCount}", task.Id, task.Name, task.RetryCount);

                // Execute command
                var result = await ExecuteCommand(task);

                // Update log with execution result
                log.SetExecutionDetails(result.StandardError, (long)result.Duration.TotalMilliseconds, result.ExitCode);

                var output = result.StandardOutput ?? string.Empty;

                _logger.LogInformation("Task {TaskId} output: {Output}", task.Id, output.Length > 1000 ? output[..1000] : output);

                if (!result.Success)
                {
                    throw new InvalidOperationException( string.IsNullOrWhiteSpace(result.StandardError)? $"Command exited with code {result.ExitCode}" : result.StandardError);
                }
                
                log.MarkAsSuccess();

                task.MarkAsActive();

                task.UpdateNextRunTime();
                task.ResetRetryCount();

               _logger.LogInformation("Task {TaskId} completed successfully. Duration={Duration}ms", task.Id, result.Duration.TotalMilliseconds);
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "Task {TaskId} execution failed", task.Id);
                
                log.MarkAsFailed(ex.Message);
                                         
                task.IncreaseRetryCount();

                if(task.RetryCount <= task.MaxRetries)
                {
                    task.MarkAsActive();
                    ScheduleRetry(task);
                }
                else
                {
                    _logger.LogError("Task {TaskId} exhausted all retries", task.Id);
                    task.MarkAsFailed(ex.Message);
                }
            }
            finally
            {
                await _repo.UpdateAsync(task);

                await _unitOfWork.SaveChangesAsync();
            }
        }

        // Trigger the task to run immediately, bypassing the scheduled time.
        public Task TriggerNow(Guid taskId)
        {
            _backgroundJobClient.Enqueue<TaskExecutionService>(
                x => x.ExecuteTask(taskId));
                
            _logger.LogInformation("Enqueue task {TaskId} for immediate execution", taskId);

            return Task.CompletedTask;
        }

        // Execute the command specified in the task and capture the output, error, exit code, and execution duration.
        private async Task<CommandExecutionResult> ExecuteCommand(ScheduledTask task)
        {
            if (string.IsNullOrWhiteSpace(task.Command))
            {
                throw new InvalidOperationException(
                    $"Task {task.Id} has empty command.");
            }

            var timeoutSeconds = _configuration.GetValue<int>("TaskExecution:TimeoutSeconds", 300);
            
            var stopwatch = Stopwatch.StartNew();

            bool isWindows = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);

            using var process = new Process();

            process.StartInfo = new ProcessStartInfo
            {
                FileName = isWindows ? "cmd.exe" : "/bin/sh",
                Arguments = isWindows ? $"/c {task.Command}" : $"-c \"{task.Command}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync();

            var errorTask = process.StandardError.ReadToEndAsync();

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));

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

                throw new TimeoutException($"Command execution exceeded {timeoutSeconds} seconds.");
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

        // Calculate the delay before the next retry based on the retry count, using an exponential backoff strategy.
        private TimeSpan GetRetryDelay(int retryCount)
        {
            return TimeSpan.FromSeconds(Math.Min(300, Math.Pow(2, retryCount) * 30));
        }

        // Schedule the task for retry by increasing the retry count and using Hangfire to schedule the next execution after a calculated delay.
        private void ScheduleRetry(ScheduledTask task)
        {
            var delay = GetRetryDelay(task.RetryCount);

            _backgroundJobClient.Schedule<TaskExecutionService>(
                s => s.ExecuteTask(task.Id),
                delay
            );

            _logger.LogWarning("Task {TaskId} scheduled retry attempt {RetryCount}/{MaxRetries} after {Delay}", task.Id, task.RetryCount, task.MaxRetries, delay);
        }

    }
}