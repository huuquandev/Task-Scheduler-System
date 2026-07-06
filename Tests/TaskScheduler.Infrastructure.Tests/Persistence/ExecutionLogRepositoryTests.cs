using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TaskScheduler.Domain.Enums;
using TaskScheduler.Domain.Entities;
using TaskScheduler.Domain.ValueObjects;
using TaskScheduler.Infrastructure.Repositories;
using TaskScheduler.Infrastructure.Tests.Common;
using Xunit;
namespace TaskScheduler.Infrastructure.Tests.Persistence
{
    public class ExecutionLogRepositoryTests : BaseInfrastructureTest
    {
        [Fact]
        public async Task AddAsync_Should_Save_ExecutionLog()
        {
            // Arrange
            var scheduledTask = new ScheduledTask(
                "Task 1",
                "Description",
                "0 * * * *",
                "backup.exe",
                3);

            TaskExecutionLog executionLog;

            // DbContext #1 → seed/setup
            using (var seedContext = Factory.CreateDbContext())
            {
                seedContext.ScheduledTasks.Add(scheduledTask);
                await seedContext.SaveChangesAsync();

                executionLog = new TaskExecutionLog(scheduledTask.Id);
                executionLog.MarkAsSuccess();

                var repository = new TaskExecutionLogRepository(seedContext);

                await repository.AddAsync(executionLog);
                await seedContext.SaveChangesAsync();
            }

            // DbContext #2 → assert/query DB 
            using (var assertContext = Factory.CreateDbContext())
            {
                var savedTask = await assertContext.TaskExecutionLogs.FirstOrDefaultAsync(x => x.Id == executionLog.Id);

                // Assert
                savedTask.Should().NotBeNull();

                savedTask!.TaskId.Should().Be(scheduledTask.Id);
                savedTask.Status.Should().Be(TaskExecutionStatus.Success);
            }
        }

        [Fact]
        public async Task GetByTaskIdAsync_Found_Should_Return_List_ExecutionLog()
        {
            // Arrange
            var scheduledTask = new ScheduledTask(
                "Task 1",
                "Description",
                "0 * * * *",
                "backup.exe",
                3);

            TaskExecutionLog executionLog1;
            TaskExecutionLog executionLog2;

            // DbContext #1 → seed/setup
            using (var seedContext = Factory.CreateDbContext())
            {
                seedContext.ScheduledTasks.Add(scheduledTask);
                await seedContext.SaveChangesAsync();

                executionLog1 = new TaskExecutionLog(scheduledTask.Id);
                executionLog1.MarkAsSuccess();

                executionLog2 = new TaskExecutionLog(scheduledTask.Id);
                executionLog2.MarkAsFailed("Failed");

                var repository = new TaskExecutionLogRepository(seedContext);

                await repository.AddAsync(executionLog1);
                await repository.AddAsync(executionLog2);

                await seedContext.SaveChangesAsync();
             }

            // DbContext #2 → assert/query DB 
            using (var assertContext = Factory.CreateDbContext())
            {
                var repository = new TaskExecutionLogRepository(assertContext);

                var logs = await repository.GetByTaskIdAsync(scheduledTask.Id);

                // Assert
                logs.Should().NotBeNull();
                logs.Count.Should().Be(2);
            }
        }

        [Fact]
        public async Task GetAllAsync_Found_Should_Return_List_ExecutionLog()
        {
            // Arrange
            var scheduledTask = new ScheduledTask(
                "Task 1",
                "Description",
                "0 * * * *",
                "backup.exe",
                3);

            TaskExecutionLog executionLog1;
            TaskExecutionLog executionLog2;
            TaskExecutionLog executionLog3;

            // DbContext #1 → seed/setup
            using (var seedContext = Factory.CreateDbContext())
            {
                seedContext.ScheduledTasks.Add(scheduledTask);
                await seedContext.SaveChangesAsync();

                executionLog1 = new TaskExecutionLog(scheduledTask.Id);
                executionLog1.MarkAsSuccess();

                executionLog2 = new TaskExecutionLog(scheduledTask.Id);
                executionLog2.MarkAsFailed("Failed");

                executionLog3 = new TaskExecutionLog(scheduledTask.Id);
                executionLog3.MarkAsSuccess();
                
                var repository = new TaskExecutionLogRepository(seedContext);

                await repository.AddAsync(executionLog1);
                await repository.AddAsync(executionLog2);
                await repository.AddAsync(executionLog3);
                await seedContext.SaveChangesAsync();
             }

            // DbContext #2 → assert/query DB 
            using (var assertContext = Factory.CreateDbContext())
            {
                var repository = new TaskExecutionLogRepository(assertContext);

                var logs = await repository.GetAllAsync();

                // Assert
                logs.Should().NotBeNull();
                logs.Count.Should().Be(3);
            }
        }

        [Fact]
        public async Task GetDetailsAsync_Found_Should_Return_ExecutionLog()
        {
            // Arrange
            var scheduledTask = new ScheduledTask(
                "Task 1",
                "Description",
                "0 * * * *",
                "backup.exe",
                3);

            TaskExecutionLog executionLog;

            // DbContext #1 → seed/setup
            using (var seedContext = Factory.CreateDbContext())
            {
                seedContext.ScheduledTasks.Add(scheduledTask);
                await seedContext.SaveChangesAsync();

                executionLog = new TaskExecutionLog(scheduledTask.Id);
                executionLog.MarkAsSuccess();
                
                var repository = new TaskExecutionLogRepository(seedContext);

                await repository.AddAsync(executionLog);

                await seedContext.SaveChangesAsync();
             }

            // DbContext #2 → assert/query DB 
            using (var assertContext = Factory.CreateDbContext())
            {
                var repository = new TaskExecutionLogRepository(assertContext);

                var logs = await repository.GetDetailsAsync(executionLog.Id);

                // Assert
                logs.Should().NotBeNull();

                logs!.TaskId.Should().Be(executionLog.TaskId);
                logs.Status.Should().Be(TaskExecutionStatus.Success);
            }
        }
    }
}