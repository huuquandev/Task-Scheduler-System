using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.InMemory;
using TaskScheduler.Domain.Entities;
using TaskScheduler.Infrastructure.Scheduling;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using Moq;
namespace TaskScheduler.Infrastructure.Tests.Scheduling
{
    public class HangfireSchedulerServiceTests
    {
        public HangfireSchedulerServiceTests()
        {
            var storage = new InMemoryStorage();

            GlobalConfiguration.Configuration.UseStorage(storage);

            JobStorage.Current = storage;

        }

        [Fact]
        public async Task ScheduleTaskAsync_Should_Create_RecurringJob()
        {
            // Arrange
            var manager = new Mock<IRecurringJobManager>();

            var service = new HangfireSchedulerService(manager.Object);

            var task = new ScheduledTask(
                "Backup",
                "Daily backup",
                "0 * * * *",   
                "backup.exe",
                3
            );

            // Act
            await service.ScheduleTaskAsync(task);

            // Assert
             manager.Verify(x =>x.AddOrUpdate<TaskJob>(
                    task.Id.ToString(),
                    It.IsAny<System.Linq.Expressions.Expression<Action<TaskJob>>>(),
                    task.CronExpression.Value,
                    It.IsAny<RecurringJobOptions>()),
                    Times.Once);
        }

        [Fact]
        public async Task UnscheduleTaskAsync_Should_Call_RemoveIfExists()
        {
            // Arrange
            var manager = new Mock<IRecurringJobManager>();

            var service = new HangfireSchedulerService(manager.Object);

            var taskId = Guid.NewGuid();

            // Act
            await service.UnscheduleTaskAsync(taskId);

            // Assert
            manager.Verify(x => x.RemoveIfExists(taskId.ToString()), Times.Once);
        }
    }
}