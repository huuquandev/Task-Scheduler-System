using FluentAssertions;
using TaskScheduler.Domain.Entities;
using TaskScheduler.Domain.Events;
using TaskScheduler.Domain.ValueObjects;
using Xunit;

namespace TaskScheduler.Domain.Tests.Entities
{
    public class ScheduledTaskTests
    {
        [Fact]
        public void Constructor_Should_Add_TaskCreatedEvent()
        {
            // Arrange & Act
            var task = new ScheduledTask(
                "Test",
                "desc",
                "0 9 * * *",
                "cmd",
                3);

            // Assert
            task.DomainEvents.Should().HaveCount(1);

            task.DomainEvents
                .First()
                .Should()
                .BeOfType<TaskCreatedEvent>();
        }

        [Fact]
        public void MarkAsFailed_Should_Add_TaskFailedEvent()
        {
            // Arrange
            var task = new ScheduledTask(
                "Test",
                "desc",
                "0 9 * * *",
                "cmd",
                3);

            // Act
            task.MarkAsFailed("timeout");

            // Assert
            task.DomainEvents.Should().HaveCount(2);

            task.DomainEvents
                .Last()
                .Should()
                .BeOfType<TaskFailedEvent>();
        }

        [Fact]
        public void ClearDomainEvents_Should_Remove_All_Events()
        {
            // Arrange
            var task = new ScheduledTask(
                "Test",
                "desc",
                "0 9 * * *",
                "cmd",
                3);

            task.MarkAsFailed("timeout");

            // Act
            task.ClearDomainEvents();

            // Assert
            task.DomainEvents.Should().BeEmpty();
        }
    }
}