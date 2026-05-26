using FluentAssertions;
using TaskScheduler.Domain.Entities;
using TaskScheduler.Domain.Events;
using TaskScheduler.Domain.ValueObjects;
using Xunit;
using TaskScheduler.Domain.Tests.Builders;

namespace TaskScheduler.Domain.Tests.Entities
{
    public class UserTests
    {
        [Fact]
        public void Constructor_Should_Create_User_When_Data_Is_Valid()
        {
            // Arrange & Act
            var user = new UserBuilder().Build();

            // Assert
            user.Id.Should().NotBeEmpty();

            user.Username.Should().Be("john");

            user.Email.Should().Be("john@test.com");

            user.PasswordHash.Should().Be("hash");

            user.IsActive.Should().BeTrue();

            user.CreatedAt.Should().NotBeNull();

            user.UpdatedAt.Should().NotBeNull();
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void Constructor_Should_Throw_When_Username_Is_Invalid(string username)
        {
            // Act
            Action act = () => new UserBuilder().WithUsername(username).Build();

            // Assert
            act.Should().Throw<ArgumentException>().WithMessage("Username cannot be empty.");
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void Constructor_Should_Throw_When_Email_Is_Invalid(string email)
        {
            // Act
            Action act = () => new UserBuilder().WithEmail(email).Build();

            // Assert
            act.Should().Throw<ArgumentException>().WithMessage("Email cannot be empty.");
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void Constructor_Should_Throw_When_PasswordHash_Is_Invalid(string passwordHash)
        {
            // Act
            Action act = () => new UserBuilder().WithPasswordHash(passwordHash).Build();

            // Assert
            act.Should().Throw<ArgumentException>().WithMessage("Password cannot be empty.");
        }

        [Fact]
        public void Deactivate_Should_Set_IsActive_To_False()
        {
            // Arrange
            var user = new UserBuilder().Build();

            // Act
            user.Deactivate();

            // Assert
            user.IsActive.Should().BeFalse();
        }

        [Fact]
        public void Activate_Should_Set_IsActive_To_True()
        {
            // Arrange
            var user = new UserBuilder().Build();

            user.Deactivate();

            // Act
            user.Activate();

            // Assert
            user.IsActive.Should().BeTrue();
        }
        [Fact]
        public void ChangePassword_Should_Update_PasswordHash()
        {
            // Arrange
            var user = new UserBuilder().Build();

            // Act
            user.ChangePassword("new-password-hash");

            // Assert
            user.PasswordHash.Should().Be("new-password-hash");
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void ChangePassword_Should_Throw_When_PasswordHash_Is_Invalid(string passwordHash)
        {
            // Arrange
            var user = new UserBuilder().Build();
            
            // Act 
            Action action = () => user.ChangePassword(passwordHash);
            
            //Assert
            action.Should().Throw<ArgumentException>().WithMessage("Password cannot be empty.");
        }

        [Fact]
        public void Activate_Should_Update_UpdatedAt()
        {
            // Arrange
            var user = new UserBuilder().Build();

            user.Deactivate();
            var oldUpdatedAt = user.UpdatedAt!.Value;

            Thread.Sleep(10);

            // Act
            user.Activate();

            // Assert
            user.UpdatedAt.Should().NotBeNull();
            user.UpdatedAt.Should().BeAfter(oldUpdatedAt);
        }

        [Fact]
        public void Deactivate_Should_Update_UpdatedAt()
        {
            // Arrange
            var user = new UserBuilder().Build();

            var oldUpdatedAt = user.UpdatedAt!.Value;

            Thread.Sleep(10);

            // Act
            user.Deactivate();

            // Assert
            user.UpdatedAt.Should().NotBeNull();
            user.UpdatedAt.Should().BeAfter(oldUpdatedAt);
        }

        [Fact]
        public void ChangePassword_Should_Update_UpdatedAt()
        {
            // Arrange
            var user = new UserBuilder().Build();

            var oldUpdatedAt = user.UpdatedAt!.Value;

            Thread.Sleep(10);

            // Act
            user.ChangePassword("new-password-hash");

            // Assert
            user.UpdatedAt.Should().NotBeNull();
            user.UpdatedAt.Should().BeAfter(oldUpdatedAt);
        }
    }
}