using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using TaskScheduler.Domain.Events;
using Xunit;

namespace TaskScheduler.Domain.Tests.Events
{
    public class TaskCreatedEventTests
    {
        [Fact]
        public void Constructor_Should_Set_Properties()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var name = "Backup Database";

            // Act
            var domainEvent = new TaskCreatedEvent(taskId, name);

            // Assert
            domainEvent.TaskId.Should().Be(taskId);
            domainEvent.Name.Should().Be(name);
        }

        [Fact]
        public void Two_TaskCreatedEvents_With_Same_Data_Should_Be_Equal()
        {
            // Arrange
            var taskId = Guid.NewGuid();

            var first = new TaskCreatedEvent(taskId, "Backup");
            var second = new TaskCreatedEvent(taskId, "Backup");

            // Assert
            first.Should().Be(second);
        }

        [Fact]
        public void Constructor_Should_Set_OccurredOn()
        {
            // Arrange
            var before = DateTime.UtcNow;

            // Act
            var domainEvent = new TaskCreatedEvent(
                Guid.NewGuid(),
                "Backup");

            // Assert
            domainEvent.OccurredOn.Should().BeOnOrAfter(before);
        }
    }
}