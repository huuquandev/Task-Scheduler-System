using FluentAssertions;
using TaskScheduler.Domain.ValueObjects;
using Xunit;
namespace TaskScheduler.Domain.Tests.ValueObjects
{
    public class CronExpressionTests
    {

        [Theory]
        [InlineData("")]
        [InlineData("abc xyz")]
        [InlineData("* *")]
        [InlineData("61 * * * *")]
        public void Create_Should_Throw_Exception_When_Cron_Is_Invalid(string invalidCron)
        {
            Action action = () => CronExpression.Create(invalidCron);

            action.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Create_Should_Succeed_When_Cron_Is_Valid()
        {
            // Arrange
            var validCron = "0 9 * * *";

            // Act
            var cron = CronExpression.Create(validCron);

            // Assert
            cron.Value.Should().Be(validCron);
        }

        [Fact]
        public void Two_CronExpressions_With_Same_Value_Should_Be_Equal()
        {
            // Arrange
            var a = CronExpression.Create("0 9 * * *");
            var b = CronExpression.Create("0 9 * * *");

            // Assert
            a.Equals(b).Should().BeTrue();
        }

        [Fact]
        public void GetNextOccurrence_Should_Return_Correct_Next_Time()
        {
            // Arrange
            var cron = CronExpression.Create("0 9 * * *");

            var currentTime = new DateTime(
                2026, 5, 26,
                8, 0, 0,
                DateTimeKind.Utc);

            // Act
            var next = cron.GetNextOccurrence(currentTime);

            // Assert
            next.Should().Be(
                new DateTime(
                    2026, 5, 26,
                    9, 0, 0,
                    DateTimeKind.Utc));
        }

        [Fact]
        public void GetNextOccurrence_WhenCurrentTimeMatchesSchedule_ShouldReturnNextDay()
        {
            // Arrange
            var cron = CronExpression.Create("0 8 * * *");

            var currentTime = new DateTime(
                2026, 5, 26,
                8, 0, 0,
                DateTimeKind.Utc);

            // Act
            var next = cron.GetNextOccurrence(currentTime);

            // Assert
            next.Should().Be(
                new DateTime(
                    2026, 5, 27,
                    8, 0, 0,
                    DateTimeKind.Utc));
        }
    }
}