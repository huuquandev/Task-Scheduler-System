using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using TaskScheduler.Application.Tasks.Commands.TriggerTask;
using taskScheduler.Application.interfaces;

namespace TaskScheduler.Application.Tests.Tasks.Commands.TriggerTask
{
    public class TriggerTaskCommandHandlerTests
    {
        [Fact]
        public async Task Handle_WhenTaskNotFound_ShouldThrowArgumentException()
        {
            // Arrange
            var repoMock = new Mock<ITaskRepository>();
            
            repoMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((ScheduledTask?)null);

            var command = new TriggerTaskCommand(Guid.NewGuid());

            var handler = new TriggerTaskHandler(repoMock.Object, Mock.Of<ITaskExecutionService>());

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

            var command = new TriggerTaskCommand(Guid.NewGuid());

            var handler = new TriggerTaskHandler(repoMock.Object, Mock.Of<ITaskExecutionService>());

            // Act
            var action = () => handler.Handle(command, CancellationToken.None);

            // Assert
            await action.Should().ThrowAsync<ArgumentException>().WithMessage("Task deleted");
        }

        [Theory]
        [InlineData("Pending")]
        [InlineData("Running")]
        [InlineData("Completed")]
        [InlineData("Paused")]
        public async Task Handle_WhenTaskCannotBeTriggered_ShouldThrowInvalidOperationException()
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

            var command = new TriggerTaskCommand(Guid.NewGuid());

            var handler = new TriggerTaskHandler(repoMock.Object, Mock.Of<ITaskExecutionService>());

            // Act
            var action = () => handler.Handle(command, CancellationToken.None);

            // Assert
            await action.Should().ThrowAsync<ArgumentException>().WithMessage("Task cannot be triggered.");
        }

        [Theory]
        [InlineData("Active")]
        [InlineData("Failed")]
        public async Task Handle_WhenRequestIsValid_ShouldExecuteTask()
        {
            // Arrange
            var repoMock = new Mock<ITaskRepository>();
            var executionServiceMock = new Mock<ITaskExecutionService>();
            var existingTask = new ScheduledTask(
                "Backup",
                "Daily backup",
                CronExpression.Create("0 0 * * *"),
                "backup.exe",
                3);

            repoMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(existingTask);

            var command = new TriggerTaskCommand(Guid.NewGuid());

            var handler = new TriggerTaskHandler(repoMock.Object, executionServiceMock.Object);

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            executionServiceMock.Verify(x => x.TriggerNow(existingTask.Id), Times.Once);
        }
    }
}