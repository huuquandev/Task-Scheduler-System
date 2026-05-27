using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using TaskScheduler.Domain.Events;
using Xunit;

namespace TaskScheduler.Domain.Tests.Events
{
    public class TaskCompletedEventTests
    {
        [Fact]
        public void Constructor_Should_Set_TaskId()
        {
            // Arrange
            var taskId = Guid.NewGuid();

            // Act
            var domainEvent = new TaskCompletedEvent(taskId);

            // Assert
            domainEvent.TaskId.Should().Be(taskId);
        }

        [Fact]
        public void Two_TaskCompletedEvents_With_Same_TaskId_Should_Be_Equal()
        {
            // Arrange
            var taskId = Guid.NewGuid();

            var first = new TaskCompletedEvent(taskId);
            var second = new TaskCompletedEvent(taskId);

            // Assert
            first.Should().Be(second);
        }

        [Fact]
        public void Constructor_Should_Set_OccurredOn()
        {
            // Arrange
            var before = DateTime.UtcNow;

            // Act
            var domainEvent = new TaskCompletedEvent(Guid.NewGuid());

            // Assert
            domainEvent.OccurredOn.Should().BeOnOrAfter(before);
        }
    }
}