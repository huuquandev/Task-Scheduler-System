using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using FluentAssertions;
using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using TaskScheduler.Application.Interfaces;
using TaskScheduler.Domain.Entities;
using TaskScheduler.Domain.Enums;
using TaskScheduler.Infrastructure.Scheduling;

namespace TaskScheduler.Infrastructure.Tests.Scheduling
{
    public class TaskExecutionServiceTests
    {
        private static IConfiguration CreateConfiguration()
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["TaskExecution:TimeoutSeconds"] = "30"
                })
                .Build();
        }

        private static TaskExecutionService CreateService(
            Mock<ITaskRepository> repoMock,
            Mock<ITaskExecutionLogRepository> logRepoMock,
            Mock<IUnitOfWork> unitOfWorkMock,
            Mock<IBackgroundJobClient> backgroundJobClientMock,
            Mock<ISchedulerService> schedulerMock)
        {
            return new TaskExecutionService(
                repoMock.Object,
                logRepoMock.Object,
                unitOfWorkMock.Object,
                Mock.Of<ILogger<TaskExecutionService>>(),
                CreateConfiguration(),
                backgroundJobClientMock.Object,
                schedulerMock.Object);
        }

        [Fact]
        public async Task ExecuteTask_WhenTaskIsPending_ShouldSkipExecution()
        {
            // Arrange
            var task = new ScheduledTask("Backup", "Daily backup", "0 0 * * *", "exit 0", 3);

            var repoMock = new Mock<ITaskRepository>();
            repoMock.Setup(x => x.GetByIdAsync(task.Id)).ReturnsAsync(task);

            var logRepoMock = new Mock<ITaskExecutionLogRepository>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();
            var backgroundJobClientMock = new Mock<IBackgroundJobClient>();
            var schedulerMock = new Mock<ISchedulerService>();

            var service = CreateService(repoMock, logRepoMock, unitOfWorkMock, backgroundJobClientMock, schedulerMock);

            // Act
            await service.ExecuteTask(task.Id);

            // Assert
            task.Status.Should().Be(ScheduledTaskStatus.Pending);
            logRepoMock.Verify(x => x.AddAsync(It.IsAny<TaskExecutionLog>()), Times.Never);
            repoMock.Verify(x => x.UpdateAsync(task), Times.Never);
            unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteTask_WhenTaskIsPaused_ShouldSkipExecution()
        {
            // Arrange
            var task = new ScheduledTask("Backup", "Daily backup", "0 0 * * *", "exit 0", 3);
            task.MarkAsActive();
            task.Pause();

            var repoMock = new Mock<ITaskRepository>();
            repoMock.Setup(x => x.GetByIdAsync(task.Id)).ReturnsAsync(task);

            var logRepoMock = new Mock<ITaskExecutionLogRepository>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();
            var backgroundJobClientMock = new Mock<IBackgroundJobClient>();
            var schedulerMock = new Mock<ISchedulerService>();

            var service = CreateService(repoMock, logRepoMock, unitOfWorkMock, backgroundJobClientMock, schedulerMock);

            // Act
            await service.ExecuteTask(task.Id);

            // Assert
            task.Status.Should().Be(ScheduledTaskStatus.Paused);
            logRepoMock.Verify(x => x.AddAsync(It.IsAny<TaskExecutionLog>()), Times.Never);
            repoMock.Verify(x => x.UpdateAsync(task), Times.Never);
        }

        [Fact]
        public async Task ExecuteTask_WhenCommandSucceeds_ShouldMarkTaskActiveAndResetRetryCount()
        {
            // Arrange
            var task = new ScheduledTask("Backup", "Daily backup", "0 0 * * *", "exit 0", 3);
            task.MarkAsActive();
            task.IncreaseRetryCount();

            var repoMock = new Mock<ITaskRepository>();
            repoMock.Setup(x => x.GetByIdAsync(task.Id)).ReturnsAsync(task);

            TaskExecutionLog? capturedLog = null;
            var logRepoMock = new Mock<ITaskExecutionLogRepository>();
            logRepoMock.Setup(x => x.AddAsync(It.IsAny<TaskExecutionLog>()))
                .Callback<TaskExecutionLog>(log => capturedLog = log);

            var unitOfWorkMock = new Mock<IUnitOfWork>();
            var backgroundJobClientMock = new Mock<IBackgroundJobClient>();
            var schedulerMock = new Mock<ISchedulerService>();

            var service = CreateService(repoMock, logRepoMock, unitOfWorkMock, backgroundJobClientMock, schedulerMock);

            // Act
            await service.ExecuteTask(task.Id);

            // Assert
            task.Status.Should().Be(ScheduledTaskStatus.Active);
            task.RetryCount.Should().Be(0);
            task.NextRunAt.Should().NotBeNull();
            capturedLog.Should().NotBeNull();
            capturedLog!.Status.Should().Be(TaskExecutionStatus.Success);
            capturedLog.ExitCode.Should().Be(0);
            schedulerMock.Verify(x => x.UnscheduleTaskAsync(task.Id), Times.Never);
        }

        [Fact]
        public async Task ExecuteTask_WhenCommandFailsAndRetriesRemain_ShouldScheduleRetryAndKeepTaskActive()
        {
            // Arrange
            var task = new ScheduledTask("Backup", "Daily backup", "0 0 * * *", "exit 1", 3);
            task.MarkAsActive();

            var repoMock = new Mock<ITaskRepository>();
            repoMock.Setup(x => x.GetByIdAsync(task.Id)).ReturnsAsync(task);

            TaskExecutionLog? capturedLog = null;
            var logRepoMock = new Mock<ITaskExecutionLogRepository>();
            logRepoMock.Setup(x => x.AddAsync(It.IsAny<TaskExecutionLog>()))
                .Callback<TaskExecutionLog>(log => capturedLog = log);

            var unitOfWorkMock = new Mock<IUnitOfWork>();
            var backgroundJobClientMock = new Mock<IBackgroundJobClient>();
            var schedulerMock = new Mock<ISchedulerService>();

            var service = CreateService(repoMock, logRepoMock, unitOfWorkMock, backgroundJobClientMock, schedulerMock);

            // Act
            await service.ExecuteTask(task.Id);

            // Assert
            task.Status.Should().Be(ScheduledTaskStatus.Active);
            task.RetryCount.Should().Be(1);
            capturedLog.Should().NotBeNull();
            capturedLog!.Status.Should().Be(TaskExecutionStatus.Failed);
            capturedLog.ExitCode.Should().Be(1);
            backgroundJobClientMock.Verify(x => x.Schedule<TaskExecutionService>(
                It.IsAny<Expression<Action<TaskExecutionService>>>(), It.IsAny<TimeSpan>()), Times.Once);
            schedulerMock.Verify(x => x.UnscheduleTaskAsync(task.Id), Times.Never);
        }

        [Fact]
        public async Task ExecuteTask_WhenCommandFailsAndRetriesExhausted_ShouldMarkFailedAndUnschedule()
        {
            // Arrange
            var task = new ScheduledTask("Backup", "Daily backup", "0 0 * * *", "exit 1", 0);
            task.MarkAsActive();

            var repoMock = new Mock<ITaskRepository>();
            repoMock.Setup(x => x.GetByIdAsync(task.Id)).ReturnsAsync(task);

            TaskExecutionLog? capturedLog = null;
            var logRepoMock = new Mock<ITaskExecutionLogRepository>();
            logRepoMock.Setup(x => x.AddAsync(It.IsAny<TaskExecutionLog>()))
                .Callback<TaskExecutionLog>(log => capturedLog = log);

            var unitOfWorkMock = new Mock<IUnitOfWork>();
            var backgroundJobClientMock = new Mock<IBackgroundJobClient>();
            var schedulerMock = new Mock<ISchedulerService>();

            var service = CreateService(repoMock, logRepoMock, unitOfWorkMock, backgroundJobClientMock, schedulerMock);

            // Act
            await service.ExecuteTask(task.Id);

            // Assert
            task.Status.Should().Be(ScheduledTaskStatus.Failed);
            capturedLog.Should().NotBeNull();
            capturedLog!.Status.Should().Be(TaskExecutionStatus.Failed);
            schedulerMock.Verify(x => x.UnscheduleTaskAsync(task.Id), Times.Once);
            backgroundJobClientMock.Verify(x => x.Schedule<TaskExecutionService>(
                It.IsAny<Expression<Action<TaskExecutionService>>>(), It.IsAny<TimeSpan>()), Times.Never);
        }
    }
}
