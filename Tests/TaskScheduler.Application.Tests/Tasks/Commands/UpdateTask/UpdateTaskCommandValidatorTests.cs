using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation.TestHelper;
using TaskScheduler.Application.Tasks.Commands.UpdateTask;

namespace TaskScheduler.Application.Tests.Tasks.Commands.UpdateTask
{
    public class UpdateTaskCommandValidatorTests
    {
        private readonly UpdateTaskCommandValidator _validator;

        public UpdateTaskCommandValidatorTests()
        {
            _validator = new UpdateTaskCommandValidator();
        }

        [Fact]
        public void Validate_WhenNameIsNull_ShouldNotHaveValidationError()
        {
            var command = new UpdateTaskCommand(
                Guid.NewGuid(),
                null,
                null,
                null,
                null,
                null);

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.Name);
        }

        [Fact]
        public void Validate_WhenNameIsValid_ShouldNotHaveValidationError()
        {
            var command = new UpdateTaskCommand(
                Guid.NewGuid(),
                "Backup",
                null,
                null,
                null,
                null);

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.Name);
        }

        
        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public void Validate_WhenNameIsEmpty_ShouldHaveValidationError(string name)
        {
            var command = new UpdateTaskCommand(
                Guid.NewGuid(),
                name,
                null,
                null,
                null,
                null);

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Name);
        }

        [Fact]
        public void Validate_WhenCommandIsNull_ShouldNotHaveValidationError()
        {
            var command = new UpdateTaskCommand(
                Guid.NewGuid(),
                null,
                null,
                null,
                null,
                null);

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.Command);
        }

        [Fact]
        public void Validate_WhenCommandIsValid_ShouldNotHaveValidationError()
        {
            var command = new UpdateTaskCommand(
                Guid.NewGuid(),
                null,
                null,
                null,
                "backup.exe",
                null);

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.Command);
        }

        [Fact]
        public void Validate_WhenCommandIsEmpty_ShouldHaveValidationError()
        {
            var command = new UpdateTaskCommand(
                Guid.NewGuid(),
                null,
                null,
                null,
                "",
                null);

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Command);
        }

        [Fact]
        public void Validate_WhenCronExpressionIsNull_ShouldNotHaveValidationError()
        {
            // Arrange
            var command = new UpdateTaskCommand(
                Guid.NewGuid(),
                null,
                null,
                null,
                null,
                null);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.CronExpression);
        }

        [Fact]
        public void Validate_WhenCronExpressionIsEmpty_ShouldHaveValidationError()
        {
            // Arrange
            var command = new UpdateTaskCommand(
                Guid.NewGuid(),
                null,
                null,
                "",
                null,
                null);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.CronExpression);
        }

        [Fact]
        public void Validate_WhenMaxRetriesIsNull_ShouldNotHaveValidationError()
        {
            // Arrange
            var command = new UpdateTaskCommand(
                Guid.NewGuid(),
                null,
                null,
                null,
                null,
                null);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors(x => x.MaxRetries);
        }

        [Fact]
        public void Validate_WhenMaxRetriesLessThanZero_ShouldHaveValidationError()
        {
            // Arrange
            var command = new UpdateTaskCommand(
                Guid.NewGuid(),
                null,
                null,
                null,
                null,
                -1);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.MaxRetries);
        }

        [Fact]
        public void Validate_WhenMaxRetriesGreaterThanTen_ShouldHaveValidationError()
        {
            // Arrange
            var command = new UpdateTaskCommand(
                Guid.NewGuid(),
                null,
                null,
                null,
                null,
                11);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.MaxRetries);
        }

        [Fact]
        public void Validate_WhenMaxRetriesIsValid_ShouldNotHaveValidationError()
        {
            // Arrange
            var command = new UpdateTaskCommand(
                Guid.NewGuid(),
                null,
                null,
                null,
                null,
                5);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.MaxRetries);
        }
    }
}