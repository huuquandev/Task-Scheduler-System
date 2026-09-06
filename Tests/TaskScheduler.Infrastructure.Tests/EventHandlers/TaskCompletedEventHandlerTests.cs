using FluentAssertions;
using MediatR;
using Moq;
using TaskScheduler.Application.Common.EventNotifications;
using TaskScheduler.Application.EventHandlers.Metrics;
using TaskScheduler.Application.EventHandlers.Notifications;
using TaskScheduler.Application.Common.Models;
using TaskScheduler.Application.Interfaces;
using TaskScheduler.Domain.Events;
using Xunit;
using Microsoft.Extensions.Configuration;

namespace TaskScheduler.Infrastructure.Tests.EventHandlers
{
    public class TaskCompletedEventHandlerTests
    {
        [Fact]
        public async Task Handle_Should_Increment_Completed_Tasks()
        {
            // Arrange
            var metricsServiceMock = new Mock<IMetricsService>();
            metricsServiceMock.Setup(x => x.IncrementCompletedTasksAsync()).Returns(Task.CompletedTask);

            var handler = new TaskCompletedMetricsHandler(metricsServiceMock.Object);
            var taskId = Guid.NewGuid();
            var notification = new DomainEventNotification<TaskCompletedEvent>(new TaskCompletedEvent(taskId));

            // Act
            await handler.Handle(notification, CancellationToken.None);

            // Assert
            metricsServiceMock.Verify(x => x.IncrementCompletedTasksAsync(), Times.Once);
        }

        [Fact]
        public async Task Handle_Failed_Should_Increment_Failed_Tasks()
        {
            // Arrange
            var metricsServiceMock = new Mock<IMetricsService>();
            metricsServiceMock.Setup(x => x.IncrementFailedTasksAsync()).Returns(Task.CompletedTask);

            var handler = new TaskFailedMetricsHandler(metricsServiceMock.Object);
            var taskId = Guid.NewGuid();
            var notification = new DomainEventNotification<TaskFailedEvent>(new TaskFailedEvent(taskId, "error"));

            // Act
            await handler.Handle(notification, CancellationToken.None);

            // Assert
            metricsServiceMock.Verify(x => x.IncrementFailedTasksAsync(), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_Send_Email_When_Task_Is_Failed_And_AdminEmail_Configured()
        {
            // Arrange
            var emailServiceMock = new Mock<IEmailService>();
            emailServiceMock.Setup(x => x.SendEmailAsync(It.IsAny<EmailMessage>())).Returns(Task.CompletedTask);

            var inMemorySettings = new Dictionary<string, string?> { ["Notifications:AdminEmail"] = "admin@example.com" };
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();

            var handler = new SendEmailHandler(emailServiceMock.Object, configuration);
            var taskId = Guid.NewGuid();
            var notification = new DomainEventNotification<TaskFailedEvent>(new TaskFailedEvent(taskId, "some error"));

            // Act
            await handler.Handle(notification, CancellationToken.None);

            // Assert
            emailServiceMock.Verify(x => x.SendEmailAsync(It.Is<EmailMessage>(m =>
                m.To == "admin@example.com" &&
                m.Subject.Contains(taskId.ToString()))), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_Not_Send_Email_When_AdminEmail_Not_Configured()
        {
            // Arrange
            var emailServiceMock = new Mock<IEmailService>();

            var inMemorySettings = new Dictionary<string, string?> { ["Notifications:AdminEmail"] = "" };
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();

            var handler = new SendEmailHandler(emailServiceMock.Object, configuration);
            var taskId = Guid.NewGuid();
            var notification = new DomainEventNotification<TaskFailedEvent>(new TaskFailedEvent(taskId, "error"));

            // Act
            await handler.Handle(notification, CancellationToken.None);

            // Assert
            emailServiceMock.Verify(x => x.SendEmailAsync(It.IsAny<EmailMessage>()), Times.Never);
        }
    }
}
