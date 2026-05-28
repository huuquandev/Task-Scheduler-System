using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation.TestHelper;
using TaskScheduler.Application.Tasks.Commands.CreateTask;

namespace TaskScheduler.Application.Tests.Tasks.Commands.CreateTask
{
    public class CreateTaskCommandValidatorTests
    {
        private readonly CreateTaskCommandValidator _validator;

        public CreateTaskCommandValidatorTests()
        {
            _validator = new CreateTaskCommandValidator();
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void Validate_WhenNameIsEmpty_ShouldHaveValidationError(string name)
        {
            // Arrange
            var command = new CreateTaskCommand(
                name,
                "Description",
                "0 0 * * *",
                "backup.exe",
                3);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Name);
        }

        [Fact]
        public void Validate_WhenNameExceeds100Characters_ShouldHaveValidationError()
        {
            // Arrange
            var command = new CreateTaskCommand(
                new string('A', 101),
                "Description",
                "0 0 * * *",
                "backup.exe",
                3);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Name);
        }

        [Fact]
        public void Validate_WhenCommandIsEmpty_ShouldHaveValidationError()
        {
            // Arrange
            var command = new CreateTaskCommand(
                "Backup",
                "Description",
                "0 0 * * *",
                "",
                3);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Command);
        }

        [Fact]
        public void Validate_WhenCronExpressionIsEmpty_ShouldHaveValidationError()
        {
            // Arrange
            var command = new CreateTaskCommand(
                "Backup",
                "Description",
                "",
                "backup.exe",
                3);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.CronExpression);
        }

        [Fact]
        public void Validate_WhenMaxRetriesLessThanZero_ShouldHaveValidationError()
        {
            // Arrange
            var command = new CreateTaskCommand(
                "Backup",
                "Description",
                "0 0 * * *",
                "backup.exe",
                -1);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.MaxRetries);
        }

        [Fact]
        public void Validate_WhenMaxRetriesGreaterThan10_ShouldHaveValidationError()
        {
            // Arrange
            var command = new CreateTaskCommand(
                "Backup",
                "Description",
                "0 0 * * *",
                "backup.exe",
                11);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.MaxRetries);
        }

        [Fact]
        public void Validate_WhenCommandIsValid_ShouldNotHaveValidationErrors()
        {
            // Arrange
            var command = new CreateTaskCommand(
                "Backup",
                "Daily backup",
                "0 0 * * *",
                "backup.exe",
                3);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }
        
    }
}