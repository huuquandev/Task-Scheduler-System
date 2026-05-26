using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using TaskScheduler.Application.Tasks.Commands.UpdateTask;

namespace TaskScheduler.Application.Tests.Tasks.Commands.UpdateTask
{
    public class UpdateTaskCommandHandlerTests
    {
        [Fact]
        public async Task Handle_WhenTaskNotFound_ShouldThrowArgumentException()
        {
            // Arrange
            var repoMock = new Mock<ITaskRepository>();

            repoMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((ScheduledTask?)null);

            var command = new UpdateTaskCommand(
                Guid.NewGuid(),
                "Updated Backup",
                "Updated description",
                "0 30 * * *",
                "updated_backup.exe",
                5);

            var handler = new UpdateTaskHandler(repoMock.Object, Mock.Of<ISchedulerService>());

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

            var command = new UpdateTaskCommand(
                Guid.NewGuid(),
                "Updated Backup",
                "Updated description",
                "0 30 * * *",
                "updated_backup.exe",
                5);

            var handler = new UpdateTaskHandler(repoMock.Object, Mock.Of<ISchedulerService>());

            // Act
            var action = () => handler.Handle(command, CancellationToken.None);

            // Assert
            await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("Task deleted");
        }

        [Fact]
        public async Task Handle_WhenRequestIsValid_ShouldUpdateTask()
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

            var command = new UpdateTaskCommand(
                Guid.NewGuid(),
                "Updated Backup",
                "Updated description",
                "0 30 * * *",
                "updated_backup.exe",
                5);

            var handler = new UpdateTaskHandler(repoMock.Object, schedulerMock.Object);

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            existingTask.Name.Should().Be("Updated Backup");
            existingTask.Description.Should().Be("Updated description");
            existingTask.CronExpression.ToString().Should().Be("0 30 * * *");
            existingTask.Command.Should().Be("updated_backup.exe");
            existingTask.MaxRetries.Should().Be(5);
        }

        [Fact]
        public async Task Handle_ShouldCallRepositoryUpdateAsync(){
            // Arrange
            var repoMock = new Mock<ITaskRepository>();

            var existingTask = new ScheduledTask(
                "Backup",
                "Daily backup",
                CronExpression.Create("0 0 * * *"),
                "backup.exe",
                3);

            repoMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(existingTask);

            var command = new UpdateTaskCommand(
                Guid.NewGuid(),
                "Updated Backup",
                "Updated description",
                "0 30 * * *",
                "updated_backup.exe",
                5);

            var handler = new UpdateTaskHandler(repoMock.Object, Mock.Of<ISchedulerService>());

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

            var command = new UpdateTaskCommand(
                Guid.NewGuid(),
                "Updated Backup",
                "Updated description",
                "0 30 * * *",
                "updated_backup.exe",
                5);

            var handler = new UpdateTaskHandler(repoMock.Object, schedulerMock.Object);

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            schedulerMock.Verify(x => x.RescheduleTaskAsync(existingTask), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldUpdateCronExpression()
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

            var command = new UpdateTaskCommand(
                Guid.NewGuid(),
                null,
                null,
                "0 15 * * *",
                null,
                null);

            var handler = new UpdateTaskHandler(repoMock.Object, schedulerMock.Object);

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            existingTask.CronExpression.ToString().Should().Be("0 15 * * *");
        }

        [Fact]
        public async Task Handle_WhenNameIsNull_ShouldKeepOldValue()
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

            var command = new UpdateTaskCommand(
                Guid.NewGuid(),
                null,
                "Updated description",
                null,
                "updated_backup.exe",
                5);

            var handler = new UpdateTaskHandler(repoMock.Object, schedulerMock.Object);

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            existingTask.Name.Should().Be("Backup");
            existingTask.Description.Should().Be("Daily backup");
        }
    }
}