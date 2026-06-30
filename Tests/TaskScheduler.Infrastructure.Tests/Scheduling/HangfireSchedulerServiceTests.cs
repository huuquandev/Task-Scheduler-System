using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.MemoryStorage;
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
            var service = new HangfireSchedulerService();

            var task = new ScheduledTask(
                "Backup",
                "Daily backup",
                "0 * * * *",   
                "backup.exe",
                3
            );

            // Act
            await service.ScheduleTaskAsync(task);

            using var connection = JobStorage.Current.GetConnection();
            var jobs = connection.GetRecurringJobs();
            var job = jobs.Single(x => x.Id == task.Id.ToString());

            // Assert
            job.Cron.Should().Be(task.CronExpression.Value);
        }

        [Fact]
        public async Task UnscheduleTaskAsync_Should_Remove_RecurringJob()
        {
            // Arrange
            var service = new HangfireSchedulerService();

            var task = new ScheduledTask(
                "Backup",
                "Daily backup",
                "0 * * * *",   
                "backup.exe",
                3
            );

            await service.ScheduleTaskAsync(task);

            // Act
            await service.UnscheduleTaskAsync(task.Id);

            using var connection = JobStorage.Current.GetConnection();
            var jobs = connection.GetRecurringJobs();

            // Assert
            jobs.Should().NotContain(x => x.Id == task.Id.ToString());
        }
    }
}