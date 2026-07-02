using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using TaskScheduler.Application.Tasks.Commands.ActiveTask;
using TaskScheduler.Application.Interfaces;
using TaskScheduler.Domain.Entities;
using TaskScheduler.Domain.Enums;

namespace TaskScheduler.Application.Tests.Tasks.Commands.ActiveTask
{
    public class ActiveTaskCommandHandlerTests
    {
        [Fact]
        public async Task Handle_WhenTaskNotFound_ShouldThrowArgumentException()
        {
            // Arrange
            var repoMock = new Mock<ITaskRepository>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();
            repoMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((ScheduledTask?)null);

            var command = new ActiveTaskCommand(Guid.NewGuid());

            var handler = new ActiveTaskHandler(repoMock.Object, unitOfWorkMock.Object, Mock.Of<ISchedulerService>());

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

            var command = new ActiveTaskCommand(Guid.NewGuid());

            var handler = new ActiveTaskHandler(repoMock.Object, unitOfWorkMock.Object, Mock.Of<ISchedulerService>());

            // Act
            var action = () => handler.Handle(command, CancellationToken.None);

            // Assert
            await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("Task deleted");
        }

        [Fact]
        public async Task Handle_WhenRequestIsValid_ShouldActivateTask()
        {
            // Arrange
            var repoMock = new Mock<ITaskRepository>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();
            var schedulerMock = new Mock<ISchedulerService>();
            var existingTask = new ScheduledTask(
                "Backup",
                "Daily backup",
                "0 0 * * *",
                "backup.exe",
                3);

            repoMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(existingTask);

            var command = new ActiveTaskCommand(Guid.NewGuid());

            var handler = new ActiveTaskHandler(repoMock.Object, unitOfWorkMock.Object, schedulerMock.Object);

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

            repoMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(existingTask);

            var command = new ActiveTaskCommand(Guid.NewGuid());

            var handler = new ActiveTaskHandler(repoMock.Object, unitOfWorkMock.Object, Mock.Of<ISchedulerService>());

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            repoMock.Verify(x => x.UpdateAsync(existingTask), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldScheduleTaskAsync()
        {
            // Arrange
            var repoMock = new Mock<ITaskRepository>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();
            var schedulerMock = new Mock<ISchedulerService>();

            var existingTask = new ScheduledTask(
                "Backup",
                "Daily backup",
                "0 0 * * *",
                "backup.exe",
                3);

            repoMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(existingTask);

            var command = new ActiveTaskCommand(Guid.NewGuid());


            var handler = new ActiveTaskHandler(repoMock.Object, unitOfWorkMock.Object, schedulerMock.Object);

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            schedulerMock.Verify(x => x.ScheduleTaskAsync(existingTask), Times.Once);
        }
    }
}