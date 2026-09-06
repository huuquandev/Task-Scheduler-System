using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TaskScheduler.Application.Common.EventNotifications;
using TaskScheduler.Application.EventHandlers.Logging;
using TaskScheduler.Domain.Events;
using Xunit;

namespace TaskScheduler.Infrastructure.Tests.EventHandlers
{
    public class TaskCreatedEventHandlerTests
    {
        private readonly Mock<ILogger<TaskCreatedLogHandler>> _loggerMock;
        private readonly TaskCreatedLogHandler _handler;

        public TaskCreatedEventHandlerTests()
        {
            _loggerMock = new Mock<ILogger<TaskCreatedLogHandler>>();
            _handler = new TaskCreatedLogHandler(_loggerMock.Object);
        }

        [Fact]
        public async Task Handle_Should_Log_Task_Created()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var taskName = "Test Task";
            var notification = new DomainEventNotification<TaskCreatedEvent>(new TaskCreatedEvent(taskId, taskName));

            // Act
            await _handler.Handle(notification, CancellationToken.None);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains(taskId.ToString())),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_Should_Return_Completed_Task()
        {
            // Arrange
            var notification = new DomainEventNotification<TaskCreatedEvent>(
                new TaskCreatedEvent(Guid.NewGuid(), "My Task"));

            // Act
            var act = async () => await _handler.Handle(notification, CancellationToken.None);

            // Assert
            await act.Should().NotThrowAsync();
        }
    }
}
