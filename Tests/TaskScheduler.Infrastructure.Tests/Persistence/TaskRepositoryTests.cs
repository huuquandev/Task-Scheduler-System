using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TaskScheduler.Domain.Entities;
using TaskScheduler.Domain.ValueObjects;
using TaskScheduler.Infrastructure.Persistence.Repositories;
using TaskScheduler.Infrastructure.Tests.Common;
using Xunit;

namespace TaskScheduler.Infrastructure.Tests.Persistence
{
    public class TaskRepositoryTests : BaseInfrastructureTest
    {
        [Fact]
        public async Task AddAsync_Should_Save_Task()
        {
            // Arrange
            var task = new ScheduledTask(
                "Backup",
                "Daily backup",
                CronExpression.Create("0 0 * * *"),
                "backup.exe",
                3);

            // DbContext #1 → seed/setup
            using (var seedContext = Factory.CreateDbContext())
            {
                var repository = new TaskRepository(seedContext);

                await repository.AddAsync(task);

                await context.SaveChangesAsync();
            }

            // DbContext #2 → assert/query DB thật
            using (var assertContext = Factory.CreateDbContext())
            {
                var savedTask = await assertContext.Tasks.FirstOrDefaultAsync(x => x.Id == task.Id);

                // Assert
                savedTask.Should().NotBeNull();

                savedTask!.Name.Should().Be("Backup");

                savedTask.Command.Should().Be("backup.exe");
            }
        }

        [Fact]
        public async Task GetByIdAsync_Found_Should_Return_Task()
        {
            // Arrange
            var task = new ScheduledTask(
                "Backup",
                "Daily backup",
                CronExpression.Create("0 0 * * *"),
                "backup.exe",
                3);

            // DbContext #1 → seed/setup
            using (var seedContext = Factory.CreateDbContext())
            {
                var repository = new TaskRepository(seedContext);

                await repository.AddAsync(task);

                await context.SaveChangesAsync();
            }

            // DbContext #2 → assert/query DB thật
            using (var assertContext = Factory.CreateDbContext())
            {
                var repository = new TaskRepository(assertContext);

                // Act
                var result = await repository.GetByIdAsync(task.Id);

                // Assert
                result.Should().NotBeNull();

                result!.Id.Should().Be(task.Id);

                result.Name.Should().Be("Backup");

                result.Description.Should()
                    .Be("Daily backup");

                result.ExecutablePath.Should()
                    .Be("backup.exe");
            }
        }
    }
}