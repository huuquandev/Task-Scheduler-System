using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation.TestHelper;
using TaskScheduler.Application.Auth.Commands.AuthLogin;

namespace TaskScheduler.Application.Tests.Auth.Commands.AuthLogin
{
    public class LoginCommandHandlerValidatorTests
    {
        private readonly LoginCommandValidator _validator;

        public LoginCommandValidatorTests()
        {
            _validator = new LoginCommandValidator();
        }

        [Fact]
        public void Validate_WhenUsernameIsEmpty_ShouldHaveValidationError()
        {
            // Arrange
            var command = new LoginCommand(
                "",
                "password");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Username);
        }

        [Fact]
        public void Validate_WhenPasswordIsEmpty_ShouldHaveValidationError()
        {
            // Arrange
            var command = new LoginCommand(
                "testuser",
                "");


            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Password);
        }

        [Fact]
        public void Validate_WhenCommandIsValid_ShouldNotHaveValidationErrors()
        {
            // Arrange
            var command = new LoginCommand(
                "testuser",
                "password");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}