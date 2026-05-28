using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using TaskScheduler.Application.Auth.Commands.AuthRegister;
using TaskScheduler.Application.Interfaces;
using TaskScheduler.Domain.Entities;

namespace TaskScheduler.Application.Tests.Auth.Commands.AuthRegister
{
    public class RegisterCommandHandlerTests
    {
        [Fact]
        public async Task Handle_WhenUsernameExists_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var repoMock = new Mock<IUserRepository>(); 
            var existingUser = new User { Username = "existinguser" };
            repoMock.Setup(x => x.UsernameExistsAsync(It.IsAny<string>())).ReturnsAsync(true);

            var command = new RegisterCommand("existinguser", "password");

            var handler = new RegisterCommandHandler(repoMock.Object, Mock.Of<ITokenService>());

            // Act
            var action = () => handler.Handle(command, CancellationToken.None);

            // Assert
            await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("Username already exists.");
        }

        [Fact]
        public async Task Handle_WhenEmailExists_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var repoMock = new Mock<IUserRepository>(); 
            var existingUser = new User { Email = "existinguser@example.com" };
            repoMock.Setup(x => x.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(true);

            var command = new RegisterCommand("existinguser", "existinguser@example.com", "password");

            var handler = new RegisterCommandHandler(repoMock.Object, Mock.Of<ITokenService>());
    
            // Act
            var action = () => handler.Handle(command, CancellationToken.None);

            // Assert
            await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("Email already exists.");
        }

        [Fact]
        public async Task Handle_WhenUserIsValid_ShouldRegisterUser()
        {
            // Arrange
            var repoMock = new Mock<IUserRepository>(); 
            var tokenServiceMock = new Mock<ITokenService>();
            User? createdUser = null;

            repoMock.Setup(x => x.UsernameExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            repoMock.Setup(x => x.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            repoMock.Setup(x => x.AddAsync(It.IsAny<User>()))
                .Callback<User>(user =>
                {
                    createdUser = user;
                });

            tokenServiceMock.Setup(x => x.HashPassword("password")).Returns("hashedPassword");
            var command = new RegisterCommand("newuser", "newuser@example.com", "password");

            var handler = new RegisterCommandHandler(repoMock.Object, tokenServiceMock.Object);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            createdUser.Should().NotBeNull();
            createdUser!.Username.Should().Be("newuser");
            createdUser.Email.Should().Be("newuser@example.com");
            createdUser.PasswordHash.Should().Be("hashedPassword");
        }

        [Fact]
        public async Task Handle_WhenUserIsValid_ShouldCallHashPassword()
        {
            // Arrange
            var repoMock = new Mock<IUserRepository>(); 
            var tokenServiceMock = new Mock<ITokenService>();
            User? createdUser = null;

            repoMock.Setup(x => x.UsernameExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            repoMock.Setup(x => x.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            repoMock.Setup(x => x.AddAsync(It.IsAny<User>()))
                .Callback<User>(user =>
                {
                    createdUser = user;
                });
            tokenServiceMock.Setup(x => x.HashPassword("password")).Returns("hashedPassword");

            var command = new RegisterCommand("newuser", "newuser@example.com", "password");

            var handler = new RegisterCommandHandler(repoMock.Object, tokenServiceMock.Object);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            createdUser!.PasswordHash.Should().NotBe("password");
            tokenServiceMock.Verify(x => x.HashPassword("password"), Times.Once);
        }
    }
}