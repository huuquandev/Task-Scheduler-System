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

                await seedContext.SaveChangesAsync();
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

                await seedContext.SaveChangesAsync();
            }

            // DbContext #2 → assert/query DB 
            using (var assertContext = Factory.CreateDbContext())
            {
                var repository = new TaskRepository(assertContext);

                // Act
                var result = await repository.GetByIdAsync(task.Id);

                // Assert
                result.Should().NotBeNull();

                result!.Id.Should().Be(task.Id);

                result.Name.Should().Be("Backup");

                result.Description.Should().Be("Daily backup");

                result.Command.Should().Be("backup.exe");
            }
        }

        [Fact]
        public async Task GetByIdAsync_NotFound_Should_Return_Null()
        {
            // Arrange
            using var context = Factory.CreateDbContext();

            var repository = new TaskRepository(context);

            var taskId = Guid.NewGuid();

            // Act
            var result = await repository.GetByIdAsync(taskId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdAsync_IsDeleted_Should_Return_Null()
        {
            // Arrange
            var task = new ScheduledTask(
                "Backup",
                "Daily backup",
                CronExpression.Create("0 0 * * *"),
                "backup.exe",
                3);

            task.SoftDelete();

            // DbContext #1 → seed delete task
            using (var seedContext = Factory.CreateDbContext())
            {
                var repository = new TaskRepository(seedContext);

                await repository.AddAsync(task);

                await seedContext.SaveChangesAsync();
            }

            // DbContext #2 → assert/query DB 
            using (var assertContext = Factory.CreateDbContext())
            {
                var repository = new TaskRepository(assertContext);

                // Act
                var result = await repository.GetByIdAsync(task.Id);

                // Assert
                result.Should().BeNull();
            }
        }

        [Fact]
        public async Task UpdateAsync_Should_Change_Task()
        {
            // Arrange
            var task = new ScheduledTask(
                "Old Name",
                "Old Description",
                CronExpression.Create("0 0 * * *"),
                "old.exe",
                3);

            // DbContext #1 → seed delete task
            using (var seedContext = Factory.CreateDbContext())
            {
                var repository = new TaskRepository(seedContext);

                await repository.AddAsync(task);

                await seedContext.SaveChangesAsync();
                task.MarkAsFailed("Test failure reason");

                await repository.UpdateAsync(task);

                await seedContext.SaveChangesAsync();
            }

            // DbContext #2 → assert/query DB 
            using (var assertContext = Factory.CreateDbContext())
            {
                var repository = new TaskRepository(assertContext);

                // Act
                var result = await repository.GetByIdAsync(task.Id);

                // Assert
                result.Status.Should().Be(ScheduledTaskStatus.Failed);
            }
        }

        [Fact]
        public async Task GetPagedAsync_Should_Return_Correct_Pages()
        {
            // Arrange
            using (var seedContext = Factory.CreateDbContext())
            {
                var repository = new TaskRepository(seedContext);
                for (int i = 1; i <= 10; i++)
                {
                    await repository.AddAsync(
                        new ScheduledTask(
                            $"Task {i}",
                            $"Description {i}",
                            CronExpression.Create("0 0 * * *"),
                            $"task{i}.exe",
                            3));
                }

                await seedContext.SaveChangesAsync();
            }

            using (var assertContext = Factory.CreateDbContext())
            {
                var repository = new TaskRepository(assertContext);

                // Act
                var firstPage = await repository.GetPagedAsync(1, 5, null);

                var secondPage = await repository.GetPagedAsync(2, 5, null);

                // Assert - page 1
                firstPage.Items.Should().HaveCount(5);

                firstPage.TotalCount.Should().Be(10);

                firstPage.Page.Should().Be(1);

                firstPage.PageSize.Should().Be(5);

                // Assert - page 2
                secondPage.Items.Should().HaveCount(5);

                secondPage.TotalCount.Should().Be(10);

                secondPage.Page.Should().Be(2);

                secondPage.PageSize.Should().Be(5);
            }
        }

        [Fact]
        public async Task GetPagedAsync_WhenFilterByStatus_ShouldReturnCorrectItems()
        {
            // Arrange
            using (var seedContext = Factory.CreateDbContext())
            {
                var repository = new TaskRepository(seedContext);
                for (int i = 1; i <= 5; i++)
                {
                    var task = new ScheduledTask(
                            $"Task {i}",
                            $"Description {i}",
                            CronExpression.Create("0 0 * * *"),
                            $"task{i}.exe",
                            3);
                    if(i%2 != 0)
                    {
                        task.MarkAsActive();
                    }
                    else
                    {
                        task.MarkAsFailed();
                    }
                    await repository.AddAsync(task);
                    
                }

                await seedContext.SaveChangesAsync();
            }

            using (var assertContext = Factory.CreateDbContext())
            {
                var repository = new TaskRepository(assertContext);

                // Act
                var filterPage = await repository.GetPagedAsync(1, 5, ScheduledTaskStatus.Active);

                // Assert - page 
                filterPage.Items.Should().HaveCount(5);
                filterPage.Items.Should().OnlyContain(x => x.Status == ScheduledTaskStatus.Active);

                filterPage.TotalCount.Should().Be(3);

                filterPage.Page.Should().Be(1);

                filterPage.PageSize.Should().Be(5);
            }
        }
    }
}