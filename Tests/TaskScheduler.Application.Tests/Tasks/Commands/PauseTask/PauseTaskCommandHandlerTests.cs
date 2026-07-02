using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using TaskScheduler.Application.Tasks.Commands.PauseTask;
using TaskScheduler.Application.Interfaces;
using TaskScheduler.Domain.Entities;
using TaskScheduler.Domain.Enums;

namespace TaskScheduler.Application.Tests.Tasks.Commands.PauseTask
{
    public class PauseTaskCommandHandlerTests
    {
        [Fact]
        public async Task Handle_WhenTaskNotFound_ShouldThrowArgumentException()
        {
            // Arrange
            var repoMock = new Mock<ITaskRepository>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();
            repoMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((ScheduledTask?)null);

            var command = new PauseTaskCommand(Guid.NewGuid());

            var handler = new PauseTaskHandler(repoMock.Object, unitOfWorkMock.Object, Mock.Of<ISchedulerService>());

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
            var unitOfWorkMock = new Mock<IUnitOfWork>();
            var existingTask = new ScheduledTask(
                "Test Task",
                "This is a test task",
                "0 0 * * *",
                "test.exe",
                3);
            existingTask.SoftDelete();

            repoMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(existingTask);

            var command = new PauseTaskCommand(Guid.NewGuid());

            var handler = new PauseTaskHandler(repoMock.Object, unitOfWorkMock.Object, Mock.Of<ISchedulerService>());

            // Act
            var action = () => handler.Handle(command, CancellationToken.None);

            // Assert
            await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("Task deleted");
        }

        [Theory]
        [InlineData(ScheduledTaskStatus.Pending)]
        [InlineData(ScheduledTaskStatus.Running)]
        [InlineData(ScheduledTaskStatus.Completed)]
        public async Task Handle_WhenTaskNotActive_ShouldThrowInvalidOperationException(ScheduledTaskStatus status)
        {
            // Arrange
            var repoMock = new Mock<ITaskRepository>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();
            var existingTask = new ScheduledTask(
                "Test Task",
                "This is a test task",
                "0 0 * * *",
                "test.exe",
                3);

            if(status == ScheduledTaskStatus.Running)
            {
                existingTask.MarkAsRunning();
            }
            else if(status == ScheduledTaskStatus.Completed)
            {
                existingTask.MarkAsCompleted();
            }

            repoMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(existingTask);

            var command = new PauseTaskCommand(Guid.NewGuid());

            var handler = new PauseTaskHandler(repoMock.Object, unitOfWorkMock.Object, Mock.Of<ISchedulerService>());

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
            var unitOfWorkMock = new Mock<IUnitOfWork>();
            var existingTask = new ScheduledTask(
                "Test Task",
                "This is a test task",
                "0 0 * * *",
                "test.exe",
                3);
            existingTask.MarkAsActive();
            repoMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(existingTask);

            var command = new PauseTaskCommand(Guid.NewGuid());

            var handler = new PauseTaskHandler(repoMock.Object, unitOfWorkMock.Object, Mock.Of<ISchedulerService>());

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
            var unitOfWorkMock = new Mock<IUnitOfWork>();   
            var existingTask = new ScheduledTask(
                "Test Task",
                "This is a test task",
                "0 0 * * *",
                "test.exe",
                3);
            existingTask.MarkAsActive();

            repoMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(existingTask);

            var command = new PauseTaskCommand(Guid.NewGuid());

            var handler = new PauseTaskHandler(repoMock.Object, unitOfWorkMock.Object, Mock.Of<ISchedulerService>());

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
            var unitOfWorkMock = new Mock<IUnitOfWork>();
            var schedulerMock = new Mock<ISchedulerService>();
            
            var existingTask = new ScheduledTask(
                "Test Task",
                "This is a test task",
                "0 0 * * *",
                "test.exe",
                3);
            existingTask.MarkAsActive();

            repoMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(existingTask);

            var command = new PauseTaskCommand(Guid.NewGuid());

            var handler = new PauseTaskHandler(repoMock.Object, unitOfWorkMock.Object, schedulerMock.Object);

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            schedulerMock.Verify(x => x.UnscheduleTaskAsync(existingTask.Id), Times.Once);
        }
    }
}