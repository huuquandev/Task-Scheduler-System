using FluentAssertions;
using TaskScheduler.Domain.Entities;
using TaskScheduler.Domain.Events;
using TaskScheduler.Domain.ValueObjects;
using Xunit;
using TaskScheduler.Domain.Tests.Builders;

namespace TaskScheduler.Domain.Tests.Entities
{
    public class ScheduledTaskTests
    {
        [Fact]
        public void Constructor_WithValidData_ShouldCreateTask()
        {
            // Arrange & Act
            var task = new ScheduledTaskBuilder().Build();

            // Assert
            task.Name.Should().Be("Backup Database");

            task.Description.Should().Be("Daily backup");

            task.Command.Should().Be("backup.exe");

            task.MaxRetries.Should().Be(3);

            task.Status.Should().Be(ScheduledTaskStatus.Pending);

            task.RetryCount.Should().Be(0);

            task.IsDeleted.Should().BeFalse();

            task.Id.Should().NotBeEmpty();
        }

        [Fact]
        public void MarkAsActive_ShouldChangeStatusToActive()
        {
            // Arrange
            var task = new ScheduledTaskBuilder().Build();

            // Act
            task.MarkAsActive();

            // Assert
            task.Status.Should().Be(ScheduledTaskStatus.Active);
        }

        [Fact]
        public void MarkAsRunning_ShouldChangeStatusToRunning()
        {
            var task = new ScheduledTaskBuilder().Build();

            task.MarkAsRunning();

            task.Status.Should().Be(ScheduledTaskStatus.Running);
        }

        [Fact]
        public void MarkAsRunning_ShouldSetLastRunAt()
        {
            var task = new ScheduledTaskBuilder().Build();

            task.MarkAsRunning();

            task.LastRunAt.Should().NotBeNull();
        }

        [Fact]
        public void Pause_WhenTaskIsActive_ShouldChangeStatusToPaused()
        {
            // Arrange
            var task = new ScheduledTaskBuilder().Build();

            task.MarkAsActive();

            // Act
            task.Pause();

            // Assert
            task.Status.Should().Be(ScheduledTaskStatus.Paused);
        }

        [Fact]
        public void Pause_WhenTaskIsActive_ShouldRaiseDomainEvent()
        {
            var task = new ScheduledTaskBuilder().Build();

            task.MarkAsActive();

            task.Pause();

            task.DomainEvents.Should().Contain(x => x is TaskPausedEvent);
        }

        [Theory]
        [InlineData(ScheduledTaskStatus.Pending)]
        [InlineData(ScheduledTaskStatus.Completed)]
        [InlineData(ScheduledTaskStatus.Failed)]
        [InlineData(ScheduledTaskStatus.Running)]
        public void Pause_WhenTaskIsNotActive_ShouldThrowException(ScheduledTaskStatus status)
        {
            // Arrange
            var task = new ScheduledTaskBuilder().Build();

            switch (status)
            {
                case ScheduledTaskStatus.Completed:
                    task.MarkAsCompleted();
                    break;

                case ScheduledTaskStatus.Failed:
                    task.MarkAsFailed("error");
                    break;

                case ScheduledTaskStatus.Running:
                    task.MarkAsRunning();
                    break;
            }

            // Act
            Action action = () => task.Pause();

            // Assert
            action.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void MarkAsFailed_ShouldChangeStatus()
        {
            var task = new ScheduledTaskBuilder().Build();

            task.MarkAsFailed("Database error");

            task.Status.Should().Be(ScheduledTaskStatus.Failed);
        }

        [Fact]
        public void MarkAsFailed_ShouldRaiseDomainEvent()
        {
            var task = new ScheduledTaskBuilder().Build();

            task.MarkAsFailed("Error");

            task.DomainEvents.Should().Contain(x => x is TaskFailedEvent);
        }

        [Fact]
        public void MarkAsCompleted_ShouldChangeStatus()
        {
            var task = new ScheduledTaskBuilder().Build();

            task.MarkAsCompleted();

            task.Status.Should().Be(ScheduledTaskStatus.Completed);
        }

        [Fact]
        public void MarkAsCompleted_ShouldRaiseDomainEvent()
        {
            var task = new ScheduledTaskBuilder().Build();

            task.MarkAsCompleted();

            task.DomainEvents.Should().Contain(x => x is TaskCompletedEvent);
        }

        [Fact]
        public void MarkAsCompleted_ShouldRaiseDomainEvent()
        {
            var task = new ScheduledTaskBuilder().Build();

            task.MarkAsCompleted();

            task.DomainEvents.Should().Contain(x => x is TaskCompletedEvent);
        }

        [Fact]
        public void SoftDelete_ShouldMarkEntityAsDeleted()
        {
            var task = new ScheduledTaskBuilder().Build();

            task.SoftDelete();

            task.IsDeleted.Should().BeTrue();
        }

        [Fact]
        public void Update_WithValidData_ShouldUpdateProperties()
        {
            var task = new ScheduledTaskBuilder().Build();

            var cron = CronExpression.Create("0 12 * * *");

            task.Update(
                "Updated Task",
                "Updated Description",
                cron,
                "updated.exe",
                5);

            task.Name.Should().Be("Updated Task");

            task.Description.Should().Be("Updated Description");

            task.Command.Should().Be("updated.exe");

            task.MaxRetries.Should().Be(5);
        }

        [Fact]
        public void Update_WithEmptyName_ShouldThrowException()
        {
            var task = new ScheduledTaskBuilder().Build();

            var cron = CronExpression.Create("0 12 * * *");

            Action action = () => task.Update(
                "",
                "Description",
                cron,
                "cmd.exe",
                5);

            action.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Update_WithNegativeMaxRetries_ShouldThrowException()
        {
            var task = new ScheduledTaskBuilder().Build();

            var cron = CronExpression.Create("0 12 * * *");

            Action action = () => task.Update(
                "Task",
                "Description",
                cron,
                "cmd.exe",
                -1);

            action.Should().Throw<ArgumentException>();
        }
    }
}