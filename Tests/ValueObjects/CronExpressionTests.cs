using FluentAssertions;
using TaskScheduler.Domain.ValueObjects;
using Xunit;

namespace TaskScheduler.Domain.Tests.ValueObjects
{
    public class CronExpressionTests
    {
        [Fact]
        public void Create_Should_Throw_Exception_When_Cron_Is_Invalid()
        {
            // Arrange
            var invalidCron = "not-a-cron";

            // Act
            Action action = () => CronExpression.Create(invalidCron);

            // Assert
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
    }
}