using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using TaskScheduler.Application.Tasks.Commands.ResumeTask;
using TaskScheduler.Application.Interfaces;
using TaskScheduler.Domain.Entities;
using TaskScheduler.Domain.Enums;

namespace TaskScheduler.Application.Tests.Tasks.Commands.ResumeTask
{
    public class ResumeTaskCommandHandlerTests
    {
        [Fact]
        public async Task Handle_WhenTaskNotFound_ShouldThrowArgumentException()
        {
            // Arrange
            var repoMock = new Mock<ITaskRepository>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            repoMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((ScheduledTask?)null);

            var command = new ResumeTaskCommand(Guid.NewGuid());

            var handler = new ResumeTaskHandler(repoMock.Object, unitOfWorkMock.Object, Mock.Of<ISchedulerService>());

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

            var deletedTask = new ScheduledTask(
                "Deleted Task",
                "This task is deleted",
                "0 0 * * *",
                "deleted.exe",
                3);
                
            deletedTask.SoftDelete(); 

            repoMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(deletedTask);

            var command = new ResumeTaskCommand(Guid.NewGuid());

            var handler = new ResumeTaskHandler(repoMock.Object, unitOfWorkMock.Object, Mock.Of<ISchedulerService>());

            // Act
            var action = () => handler.Handle(command, CancellationToken.None);

            // Assert
            await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("Task deleted");
        }

        [Theory]
        [InlineData(ScheduledTaskStatus.Pending)]
        [InlineData(ScheduledTaskStatus.Completed)]
        [InlineData(ScheduledTaskStatus.Running)]
        [InlineData(ScheduledTaskStatus.Active)]
        public async Task Handle_WhenTaskIsNotPaused_ShouldThrowInvalidOperationException(ScheduledTaskStatus status)
        {
            // Arrange
            var repoMock = new Mock<ITaskRepository>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            var task = new ScheduledTask(
                "Test Task",
                "This task is not paused",
                "0 0 * * *",
                "test.exe",
                3);
            if(status == ScheduledTaskStatus.Completed)
                task.MarkAsCompleted();
            else if(status == ScheduledTaskStatus.Running)
                task.MarkAsRunning();
            else if(status == ScheduledTaskStatus.Active)
                task.MarkAsActive();

            repoMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(task);

            var command = new ResumeTaskCommand(Guid.NewGuid());

            var handler = new ResumeTaskHandler(repoMock.Object, unitOfWorkMock.Object, Mock.Of<ISchedulerService>());

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
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            var existingTask = new ScheduledTask(
                "Backup",
                "Daily backup",
                "0 0 * * *",
                "backup.exe",
                3);
            existingTask.MarkAsActive();
            existingTask.Pause();
            repoMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(existingTask);

            var command = new ResumeTaskCommand(Guid.NewGuid());

            var handler = new ResumeTaskHandler(repoMock.Object, unitOfWorkMock.Object, schedulerMock.Object);

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            existingTask.Status.Should().Be(ScheduledTaskStatus.Active);
        }

        [Fact]
        public async Task Handle_ShouldCallRepositoryUpdateAsync()
        {
            // Arrange
            var repoMock = new Mock<ITaskRepository>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            var existingTask = new ScheduledTask(
                "Backup",
                "Daily backup",
                "0 0 * * *",
                "backup.exe",
                3);

            existingTask.MarkAsActive();
            existingTask.Pause();

            repoMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(existingTask);

            var command = new ResumeTaskCommand(Guid.NewGuid());

            var handler = new ResumeTaskHandler(repoMock.Object, unitOfWorkMock.Object, Mock.Of<ISchedulerService>());

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
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            var existingTask = new ScheduledTask(
                "Backup",
                "Daily backup",
                "0 0 * * *",
                "backup.exe",
                3);

            existingTask.MarkAsActive();
            existingTask.Pause();

            repoMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(existingTask);

            var command = new ResumeTaskCommand(Guid.NewGuid());

            var handler = new ResumeTaskHandler(repoMock.Object, unitOfWorkMock.Object, schedulerMock.Object);

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            schedulerMock.Verify(x => x.RescheduleTaskAsync(existingTask), Times.Once);
        }
    }
}