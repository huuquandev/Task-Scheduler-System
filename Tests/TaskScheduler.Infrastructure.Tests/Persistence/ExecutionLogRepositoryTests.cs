using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TaskScheduler.Domain.Enums;
using TaskScheduler.Domain.Entities;
using TaskScheduler.Domain.ValueObjects;
using TaskScheduler.Infrastructure.Persistence.Repositories;
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
            var task = new TaskExecutionLog(
                Guid.NewGuid(),
                Guid.NewGuid(),
                DateTime.UtcNow,
                "Success"
            );

            // DbContext #1 → seed/setup
            using (var seedContext = Factory.CreateDbContext())
            {
                var repository = new TaskExecutionLogRepository(seedContext);

                await repository.AddAsync(task);

                await seedContext.SaveChangesAsync();
            }

            // DbContext #2 → assert/query DB 
            using (var assertContext = Factory.CreateDbContext())
            {
                var savedTask = await assertContext.TaskExecutionLogs.FirstOrDefaultAsync(x => x.Id == task.Id);

                // Assert
                savedTask.Should().NotBeNull();

                savedTask!.TaskId.Should().Be(task.TaskId);
                savedTask.ErrorMessage.Should().Be("Success");
            }
        }

        [Fact]
        public async Task GetByTaskIdAsync_Found_Should_Return_List_ExecutionLog()
        {
            // Arrange
            var taskId = Guid.NewGuid();

            var task1 = new TaskExecutionLog(
                taskId,
                Guid.NewGuid(),
                DateTime.UtcNow,
                "Success"
            );

            var task2 = new TaskExecutionLog(
                taskId,
                Guid.NewGuid(),
                DateTime.UtcNow,
                "Failed"
            );

            // DbContext #1 → seed/setup
            using (var seedContext = Factory.CreateDbContext())
            {
                var repository = new TaskExecutionLogRepository(seedContext);

                await repository.AddAsync(task1);
                await repository.AddAsync(task2);

                await seedContext.SaveChangesAsync();
             }

            // DbContext #2 → assert/query DB 
            using (var assertContext = Factory.CreateDbContext())
            {
                var repository = new TaskExecutionLogRepository(assertContext);

                var logs = await repository.GetByTaskIdAsync(taskId);

                // Assert
                logs.Should().NotBeNull();
                logs.Count.Should().Be(2);
            }
        }

        [Fact]
        public async Task GetAllAsync_Found_Should_Return_List_ExecutionLog()
        {
            // Arrange
            var taskId = Guid.NewGuid();

            var task1 = new TaskExecutionLog(
                taskId,
                Guid.NewGuid(),
                DateTime.UtcNow,
                "Success"
            );

            var task2 = new TaskExecutionLog(
                taskId,
                Guid.NewGuid(),
                DateTime.UtcNow,
                "Failed"
            );

            var task3 = new TaskExecutionLog(
                taskId,
                Guid.NewGuid(),
                DateTime.UtcNow,
                "Running"
            );

            // DbContext #1 → seed/setup
            using (var seedContext = Factory.CreateDbContext())
            {
                var repository = new TaskExecutionLogRepository(seedContext);

                await repository.AddAsync(task1);
                await repository.AddAsync(task2);
                await repository.AddAsync(task3);
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
        public async Task GetByTaskIdAsync_Found_Should_Return_List_ExecutionLog()
        {
            // Arrange
            var logId = Guid.NewGuid();
            var task = new TaskExecutionLog(
                logId,
                Guid.NewGuid(),
                DateTime.UtcNow,
                "Success"
            );

            // DbContext #1 → seed/setup
            using (var seedContext = Factory.CreateDbContext())
            {
                var repository = new TaskExecutionLogRepository(seedContext);

                await repository.AddAsync(task);

                await seedContext.SaveChangesAsync();
             }

            // DbContext #2 → assert/query DB 
            using (var assertContext = Factory.CreateDbContext())
            {
                var repository = new TaskExecutionLogRepository(assertContext);

                var logs = await repository.GetDetailsAsync(logId);

                // Assert
                logs.Should().NotBeNull();

                logs!.TaskId.Should().Be(task.TaskId);
                logs.ErrorMessage.Should().Be("Success");
            }
        }
    }
}