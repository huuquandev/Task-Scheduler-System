using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using TaskScheduler.Application.Tasks.Commands.ResumeTask;

namespace TaskScheduler.Application.Tests.Tasks.Commands.ResumeTask
{
    public class ResumeTaskCommandHandlerTests
    {
        [Fact]
        public async Task Handle_WhenTaskNotFound_ShouldThrowArgumentException()
        {
            // Arrange
            var repoMock = new Mock<ITaskRepository>();
            
            repoMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((ScheduledTask?)null);

            var command = new ActiveTaskCommand(Guid.NewGuid());

            var handler = new ActiveTaskHandler(repoMock.Object, Mock.Of<ISchedulerService>());

            // Act
            var action = () => handler.Handle(command, CancellationToken.None);

            // Assert
            await action.Should().ThrowAsync<ArgumentException>().WithMessage("Task not found.");
        }

        [Fact]
        public async Task Handle_WhenTaskIsDeleted_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var repoMock = new Mock<ITaskRepository>();
            
            var deletedTask = new ScheduledTask(
                "Deleted Task",
                "This task is deleted",
                CronExpression.Create("0 0 * * *"),
                "deleted.exe",
                3)
            {
                IsDeleted = true
            };

            repoMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(deletedTask);

            var command = new ActiveTaskCommand(Guid.NewGuid());

            var handler = new ActiveTaskHandler(repoMock.Object, Mock.Of<ISchedulerService>());

            // Act
            var action = () => handler.Handle(command, CancellationToken.None);

            // Assert
            await action.Should().ThrowAsync<ArgumentException>().WithMessage("Task deleted");
        }

        [theory]
        [InlineData("Pending")]
        [InlineData("Completed")]
        [InlineData("Running")]
        [InlineData("Active")]
        public async Task Handle_WhenTaskIsNotPaused_ShouldThrowInvalidOperationException(string status)
        {
            // Arrange
            var repoMock = new Mock<ITaskRepository>();
            
            var task = new ScheduledTask(
                "Test Task",
                "This task is not paused",
                CronExpression.Create("0 0 * * *"),
                "test.exe",
                3)
            {
                Status = status
            };

            repoMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(task);

            var command = new ActiveTaskCommand(Guid.NewGuid());

            var handler = new ActiveTaskHandler(repoMock.Object, Mock.Of<ISchedulerService>());

            // Act
            var action = () => handler.Handle(command, CancellationToken.None);

            // Assert
            await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("Only paused tasks can be resumed.");
        }

        [Fact]
        public async Task Handle_WhenRequestIsPaused_ShouldResumeTask()
        {
            // Arrange
            var repoMock = new Mock<ITaskRepository>();
            var schedulerMock = new Mock<ISchedulerService>();
            var existingTask = new ScheduledTask(
                "Backup",
                "Daily backup",
                CronExpression.Create("0 0 * * *"),
                "backup.exe",
                3);

            repoMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(existingTask);

            var command = new ActiveTaskCommand(Guid.NewGuid());

            var handler = new ActiveTaskHandler(repoMock.Object, schedulerMock.Object);

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            existingTask.IsActive.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_ShouldCallRepositoryUpdateAsync()
        {
            // Arrange
            var repoMock = new Mock<ITaskRepository>();

            var existingTask = new ScheduledTask(
                "Backup",
                "Daily backup",
                CronExpression.Create("0 0 * * *"),
                "backup.exe",
                3);

            repoMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(existingTask);

            var command = new ActiveTaskCommand(Guid.NewGuid());

            var handler = new ActiveTaskHandler(repoMock.Object, Mock.Of<ISchedulerService>());

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            repoMock.Verify(x => x.UpdateAsync(existingTask), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldRescheduleTask()
        {
            // Arrange
            var repoMock = new Mock<ITaskRepository>();
            var schedulerMock = new Mock<ISchedulerService>();

            var existingTask = new ScheduledTask(
                "Backup",
                "Daily backup",
                CronExpression.Create("0 0 * * *"),
                "backup.exe",
                3);

            repoMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(existingTask);

            var command = new ActiveTaskCommand(Guid.NewGuid());


            var handler = new ActiveTaskHandler(repoMock.Object, schedulerMock.Object);

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            schedulerMock.Verify(x => x.RescheduleTaskAsync(existingTask), Times.Once);
        }
    }
}