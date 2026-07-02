using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using TaskScheduler.Application.Auth.Commands.AuthLogin;
using TaskScheduler.Application.Interfaces;
using TaskScheduler.Domain.Entities;

namespace TaskScheduler.Application.Tests.Auth.Commands.AuthLogin
{
    public class LoginHandlerTests
    {
        [Fact]
        public async Task Handle_WhenUserNotFound_ShouldThrowUnauthorizedAccessException()
        {
            // Arrange
            var repoMock = new Mock<IUserRepository>();

            repoMock.Setup(x => x.GetByUsernameAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

            var command = new LoginCommand("testuser", "password");

            var handler = new LoginHandler(repoMock.Object, Mock.Of<ITokenService>());

            // Act
            var action = () => handler.Handle(command, CancellationToken.None);

            // Assert
            await action.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("Invalid username or password.");
        }

        [Fact]
        public async Task Handle_WhenUserInvalidPassword_ShouldThrowUnauthorizedAccessException()
        {
            // Arrange
            var repoMock = new Mock<IUserRepository>();
            var existingUser = new User
            {
                Username = "testuser",
                PasswordHash = "hashedpassword",
                IsActive = true
            };
            repoMock.Setup(x => x.GetByUsernameAsync(It.IsAny<string>())).ReturnsAsync(existingUser);

            var command = new LoginCommand("testuser", "wrongpassword");

            var handler = new LoginHandler(repoMock.Object, Mock.Of<ITokenService>());

            // Act
            var action = () => handler.Handle(command, CancellationToken.None);

            // Assert
            await action.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("Invalid username or password.");
        }

        [Fact]
        public async Task Handle_WhenUserIsDeactivated_ShouldThrowUnauthorizedAccessException()
        {
            // Arrange
            var repoMock = new Mock<IUserRepository>();
            var tokenServiceMock = new Mock<ITokenService>();
            var existingUser = new User
            {
                Username = "testuser",
                PasswordHash = "hashedpassword",
                IsActive = false
            };
            repoMock.Setup(x => x.GetByUsernameAsync(It.IsAny<string>())).ReturnsAsync(existingUser);
            tokenServiceMock.Setup(x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

            var command = new LoginCommand("testuser", "hashedpassword");

            var handler = new LoginHandler(repoMock.Object, tokenServiceMock.Object);

            // Act
            var action = () => handler.Handle(command, CancellationToken.None);

            // Assert
            await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("Account is disabled.");
        }

        [Fact]
        public async Task Handle_WhenUserIsValid_ShouldReturnToken()
        {
            // Arrange
            var repoMock = new Mock<IUserRepository>();
            var tokenServiceMock = new Mock<ITokenService>();
            var existingUser = new User
            {
                Username = "testuser",
                PasswordHash = "hashedpassword",
                IsActive = true
            };
            repoMock.Setup(x => x.GetByUsernameAsync(It.IsAny<string>())).ReturnsAsync(existingUser);
            tokenServiceMock.Setup(x => x.GenerateJwtToken(existingUser)).Returns("fake-jwt-token");
            tokenServiceMock.Setup(x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
            var command = new LoginCommand("testuser", "hashedpassword");

            var handler = new LoginHandler(repoMock.Object, tokenServiceMock.Object);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();

            result.Token.Should().Be("fake-jwt-token");

            result.Username.Should().Be("testuser");
        }

        [Fact]
        public async Task Handle_WhenUserIsValid_ShouldCallGenerateJwtToken()
        {
            // Arrange
            var repoMock = new Mock<IUserRepository>();
            var tokenServiceMock = new Mock<ITokenService>();
            var existingUser = new User
            {
                Username = "testuser",
                PasswordHash = "hashedpassword",
                IsActive = true
            };
            repoMock.Setup(x => x.GetByUsernameAsync(It.IsAny<string>())).ReturnsAsync(existingUser);
            tokenServiceMock.Setup(x => x.GenerateJwtToken(existingUser)).Returns("fake-jwt-token");
            tokenServiceMock.Setup(x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

            var command = new LoginCommand("testuser", "hashedpassword");

            var handler = new LoginHandler(repoMock.Object, tokenServiceMock.Object);

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            tokenServiceMock.Verify(x => x.GenerateJwtToken(existingUser), Times.Once);
        }
    }
}