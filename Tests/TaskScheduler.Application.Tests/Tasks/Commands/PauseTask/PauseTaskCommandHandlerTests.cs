using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using TaskScheduler.Application.Tasks.Commands.PauseTask;

namespace TaskScheduler.Application.Tests.Tasks.Commands.PauseTask
{
    public class PauseTaskCommandHandlerTests
    {
        [Fact]
        public async Task Handle_WhenTaskNotFound_ShouldThrowArgumentException()
        {
            // Arrange
            var repoMock = new Mock<ITaskRepository>();
            
            repoMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((ScheduledTask?)null);

            var command = new PauseTaskCommand(Guid.NewGuid());

            var handler = new PauseTaskHandler(repoMock.Object, Mock.Of<ISchedulerService>());

            // Act
            var action = () => handler.Handle(command, CancellationToken.None);

            // Assert
            await action.Should().ThrowAsync<ArgumentException>().WithMessage("Task not found.");
        }

        [Theory]
        [InlineData("Pending")]
        [InlineData("Running")]
        [InlineData("Completed")]
        public async Task Handle_WhenTaskNotActive_ShouldThrowArgumentException(string status)
        {
            // Arrange
            var repoMock = new Mock<ITaskRepository>();
            var existingTask = new ScheduledTask(
                "Test Task",
                "This is a test task",
                CronExpression.Create("0 0 * * *"),
                "test.exe",
                3)
            {
                Status = status
            };
            repoMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(existingTask);

            var command = new PauseTaskCommand(Guid.NewGuid());

            var handler = new PauseTaskHandler(repoMock.Object, Mock.Of<ISchedulerService>());

            // Act
            var action = () => handler.Handle(command, CancellationToken.None);

            // Assert
            await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("Only active task can be paused.");
        }
        [Fact]
        public async Task Handle_WhenTaskIsActive_ShouldPauseTask()
        {
            // Arrange
            var repoMock = new Mock<ITaskRepository>();
            var existingTask = new ScheduledTask(
                "Test Task",
                "This is a test task",
                CronExpression.Create("0 0 * * *"),
                "test.exe",
                3)
            {
                Status = ScheduledTaskStatus.Active
            };
            repoMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(existingTask);

            var command = new PauseTaskCommand(Guid.NewGuid());

            var handler = new PauseTaskHandler(repoMock.Object, Mock.Of<ISchedulerService>());

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            existingTask.Status.Should().Be(ScheduledTaskStatus.Paused);
        }

        [Fact]
        public async Task Handle_ShouldCallRepositoryUpdateAsync()
        {
            // Arrange
            var repoMock = new Mock<ITaskRepository>();

            var existingTask = new ScheduledTask(
                "Test Task",
                "This is a test task",
                CronExpression.Create("0 0 * * *"),
                "test.exe",
                3)
            {
                Status = ScheduledTaskStatus.Active
            };

            repoMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(existingTask);

            var command = new PauseTaskCommand(Guid.NewGuid());

            var handler = new PauseTaskHandler(repoMock.Object, Mock.Of<ISchedulerService>());

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            repoMock.Verify(x => x.UpdateAsync(existingTask), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldUnscheduleTask()
        {
            // Arrange
            var repoMock = new Mock<ITaskRepository>();
            var schedulerMock = new Mock<ISchedulerService>();
            
            var existingTask = new ScheduledTask(
                "Test Task",
                "This is a test task",
                CronExpression.Create("0 0 * * *"),
                "test.exe",
                3)
            {
                Status = ScheduledTaskStatus.Active
            };

            repoMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(existingTask);

            var command = new PauseTaskCommand(Guid.NewGuid());

            var handler = new PauseTaskHandler(repoMock.Object, schedulerMock.Object);

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            schedulerMock.Verify(x => x.UnscheduleTaskAsync(existingTask), Times.Once);
        }
    }
}