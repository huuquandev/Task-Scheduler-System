using FluentAssertions;
using TaskScheduler.Domain.Entities;
using Xunit;
using TaskScheduler.Domain.Tests.Builders;

namespace TaskScheduler.Domain.Tests.Entities
{
    public class ExecutionLogTests
    {
        // Constructor Tests
        [Fact]
        public void Constructor_WithValidTaskId_ShouldCreateLog()
        {
            // Arrange
            var taskId = Guid.NewGuid();

            // Act
            var log = new TaskExecutionLog(taskId);

            // Assert
            log.Id.Should().NotBeEmpty();

            log.TaskId.Should().Be(taskId);

            log.Status.Should().Be(TaskExecutionStatus.Running);

            log.StartedAt.Should().NotBe(default(DateTime));

            log.FinishedAt.Should().BeNull();

            log.ErrorMessage.Should().BeNull();

            log.DurationMs.Should().BeNull();
        }

        // MarkAsSuccess()
        [Fact]
        public void MarkAsSuccess_WhenRunning_ShouldChangeStatusToSuccess()
        {
            // Arrange
            var log = new TaskExecutionLogBuilder().Build();

            // Act
            log.MarkAsSuccess();

            // Assert
            log.Status.Should().Be(TaskExecutionStatus.Success);
        }

        [Fact]
        public void MarkAsSuccess_ShouldSetFinishedAt()
        {
            var log = new TaskExecutionLogBuilder().Build();

            log.MarkAsSuccess();

            log.FinishedAt.Should().NotBeNull();
        }

        [Fact]
        public void MarkAsSuccess_ShouldCalculateDuration()
        {
            var log = new TaskExecutionLogBuilder().Build();

            Thread.Sleep(50);

            log.MarkAsSuccess();

            log.DurationMs.Should().BeGreaterThan(0);
        }

        [Fact]
        public void MarkAsSuccess_WhenNotRunning_ShouldThrowException()
        {
            // Arrange
            var log = new TaskExecutionLogBuilder().Build();

            log.MarkAsSuccess();

            // Act
            Action action = () => log.MarkAsSuccess();

            // Assert
            action.Should().Throw<InvalidOperationException>().WithMessage("Only running task can be completed.");
        }

        // MarkAsFailed()
        [Fact]
        public void MarkAsFailed_WhenRunning_ShouldChangeStatusToFailed()
        {
            var log = new TaskExecutionLogBuilder().Build();

            log.MarkAsFailed("Database Error");

            log.Status.Should().Be(TaskExecutionStatus.Failed);
        }

        [Fact]
        public void MarkAsFailed_ShouldStoreErrorMessage()
        {
            var log = new TaskExecutionLogBuilder().Build();

            log.MarkAsFailed("Database Error");

            log.ErrorMessage.Should().Be("Database Error");
        }

        [Fact]
        public void MarkAsFailed_ShouldSetFinishedAt()
        {
            var log = new TaskExecutionLogBuilder().Build();

            log.MarkAsFailed("Error");

            log.FinishedAt.Should().NotBeNull();
        }

        [Fact]
        public void MarkAsFailed_ShouldCalculateDuration()
        {
            var log = new TaskExecutionLogBuilder().Build();

            Thread.Sleep(50);

            log.MarkAsFailed("Error");

            log.DurationMs.Should().BeGreaterThan(0);
        }

        [Fact]
        public void MarkAsFailed_WithEmptyMessage_ShouldThrowException()
        {
            var log = new TaskExecutionLogBuilder().Build();

            Action action = () => log.MarkAsFailed("");

            action.Should().Throw<ArgumentException>().WithMessage("Error message cannot be empty.");
        }

        [Fact]
        public void MarkAsFailed_WithNullMessage_ShouldThrowException()
        {
            var log = new TaskExecutionLogBuilder().Build();

            Action action = () => log.MarkAsFailed(null!);

            action.Should().Throw<ArgumentException>().WithMessage("Error message cannot be empty.");
        }

        [Fact]
        public void MarkAsFailed_WhenNotRunning_ShouldThrowException()
        {
            var log = new TaskExecutionLogBuilder().Build();

            log.MarkAsSuccess();

            Action action = () => log.MarkAsFailed("Error");

            action.Should().Throw<InvalidOperationException>().WithMessage("Only running task can fail.");
        }

        // Retry()
        [Fact]
        public void Retry_ShouldResetFinishedAt()
        {
            var log = new TaskExecutionLogBuilder().Build();

            log.MarkAsFailed("Error");

            log.Retry();

            log.FinishedAt.Should().BeNull();
        }

        [Fact]
        public void Retry_ShouldClearErrorMessage()
        {
            var log = new TaskExecutionLogBuilder().Build();

            log.MarkAsFailed("Database Error");

            log.Retry();

            log.ErrorMessage.Should().BeNull();
        }

        [Fact]
        public void Retry_ShouldClearDuration()
        {
            var log = new TaskExecutionLogBuilder().Build();

            log.MarkAsFailed("Error");

            log.Retry();

            log.DurationMs.Should().BeNull();
        }

        [Fact]
        public void Retry_ShouldChangeStatusToRunning()
        {
            var log = new TaskExecutionLogBuilder().Build();

            log.MarkAsFailed("Error");

            log.Retry();

            log.Status.Should().Be(TaskExecutionStatus.Running);
        }

        [Fact]
        public void Retry_ShouldUpdateStartedAt()
        {
            var log = new TaskExecutionLogBuilder().Build();

            var originalStartedAt = log.StartedAt;

            log.MarkAsFailed("Error");

            Thread.Sleep(20);

            log.Retry();

            log.StartedAt.Should().BeAfter(originalStartedAt);
        }
    }
}