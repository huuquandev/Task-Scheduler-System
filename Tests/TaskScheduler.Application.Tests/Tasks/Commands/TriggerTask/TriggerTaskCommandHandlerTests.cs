using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using TaskScheduler.Application.Tasks.Commands.TriggerTask;
using TaskScheduler.Application.Interfaces;
using TaskScheduler.Domain.Entities;
using TaskScheduler.Domain.Enums;

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
            await action.Should().ThrowAsync<KeyNotFoundException>().WithMessage("Task not found.");
        }

        [Fact]
        public async Task Handle_WhenTaskIsDeleted_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var repoMock = new Mock<ITaskRepository>();
            
            var deletedTask = new ScheduledTask(
                "Deleted Task",
                "This task is deleted",
                "0 0 * * *",
                "deleted.exe",
                3);
            deletedTask.SoftDelete();

            repoMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(deletedTask);

            var command = new TriggerTaskCommand(Guid.NewGuid());

            var handler = new TriggerTaskHandler(repoMock.Object, Mock.Of<ITaskExecutionService>());

            // Act
            var action = () => handler.Handle(command, CancellationToken.None);

            // Assert
            await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("Task deleted");
        }

        [Theory]
        [InlineData(ScheduledTaskStatus.Pending)]
        [InlineData(ScheduledTaskStatus.Running)]
        [InlineData(ScheduledTaskStatus.Completed)]
        [InlineData(ScheduledTaskStatus.Paused)]
        public async Task Handle_WhenTaskCannotBeTriggered_ShouldThrowInvalidOperationException(ScheduledTaskStatus status)
        {
            // Arrange
            var repoMock = new Mock<ITaskRepository>();
            
            var existingTask = new ScheduledTask(
                "Deleted Task",
                "This task is deleted",
                "0 0 * * *",
                "deleted.exe",
                3);

            repoMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(existingTask);

            var command = new TriggerTaskCommand(Guid.NewGuid());

            var handler = new TriggerTaskHandler(repoMock.Object, Mock.Of<ITaskExecutionService>());

            // Act
            var action = () => handler.Handle(command, CancellationToken.None);

            // Assert
            await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("Only Active or Failed tasks can be triggered manually.");
        }

        [Theory]
        [InlineData(ScheduledTaskStatus.Active)]
        [InlineData(ScheduledTaskStatus.Failed)]
        public async Task Handle_WhenRequestIsValid_ShouldExecuteTask(ScheduledTaskStatus status)
        {
            // Arrange
            var repoMock = new Mock<ITaskRepository>();
            var executionServiceMock = new Mock<ITaskExecutionService>();
            var existingTask = new ScheduledTask(
                "Backup",
                "Daily backup",
                "0 0 * * *",
                "backup.exe",
                3);
            if(status == ScheduledTaskStatus.Failed)
            {
                existingTask.MarkAsFailed("Task Failed");
            }
            else
            {
                existingTask.MarkAsActive();
            }
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