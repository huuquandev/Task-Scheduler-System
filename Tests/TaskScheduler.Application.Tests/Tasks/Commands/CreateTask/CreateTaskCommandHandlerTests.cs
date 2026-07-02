using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using TaskScheduler.Application.Tasks.Commands.CreateTask;
using TaskScheduler.Application.Interfaces;
using TaskScheduler.Domain.Entities;

namespace TaskScheduler.Application.Tests.Tasks.Commands.CreateTask
{
    public class CreateTaskCommandHandlerTests
    {
        [Fact]
        public async Task Handle_ShouldReturnCreatedTaskId()
        {
            // Arrange
            var repoMock = new Mock<ITaskRepository>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            var command = new CreateTaskCommand(
                "Backup",
                "Daily backup",
                "0 0 * * *",
                "backup.exe",
                3);

            var handler = new CreateTaskHandler(repoMock.Object, unitOfWorkMock.Object);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeEmpty();
        }

        [Fact]
        public async Task Handle_ShouldCallAddAsyncOnce()
        {
            // Arrange
            var repoMock = new Mock<ITaskRepository>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            var command = new CreateTaskCommand(
                "Backup",
                "Daily backup",
                "0 0 * * *",
                "backup.exe",
                3);

            var handler = new CreateTaskHandler(repoMock.Object, unitOfWorkMock.Object);

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            repoMock.Verify(x => x.AddAsync(It.IsAny<ScheduledTask>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldCreateTaskWithCorrectProperties()
        {
            // Arrange
            var repoMock = new Mock<ITaskRepository>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();
            ScheduledTask? createdTask = null;

            repoMock.Setup(x => x.AddAsync(It.IsAny<ScheduledTask>()))
                .Callback<ScheduledTask>(task =>
                {
                    createdTask = task;
                });

            var command = new CreateTaskCommand(
                "Backup",
                "Daily backup",
                "0 0 * * *",
                "backup.exe",
                3);

            var handler = new CreateTaskHandler(repoMock.Object, unitOfWorkMock.Object);

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            createdTask.Should().NotBeNull();

            createdTask!.Name.Should().Be("Backup");

            createdTask!.Description.Should().Be("Daily backup");

            createdTask!.Command.Should().Be("backup.exe");

            createdTask!.MaxRetries.Should().Be(3);
        }

        [Fact]
        public async Task Handle_ShouldSetNextRunTime()
        {
            // Arrange
            var repoMock = new Mock<ITaskRepository>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();

            ScheduledTask? createdTask = null;

            repoMock.Setup(x => x.AddAsync(It.IsAny<ScheduledTask>()))
                .Callback<ScheduledTask>(task =>
                {
                    createdTask = task;
                });

            var command = new CreateTaskCommand(
                "Backup",
                "Daily backup",
                "0 0 * * *",
                "backup.exe",
                3);
            var handler = new CreateTaskHandler(repoMock.Object, unitOfWorkMock.Object);

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            createdTask!.NextRunAt.Should().NotBeNull();
        }
    }
}