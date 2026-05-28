using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation.Results;
using FluentAssertions;
using Moq;
using TaskScheduler.Application.Common.Behaviors;
using TaskScheduler.Application.Tasks.Commands.CreateTask;
namespace TaskScheduler.Application.Tests.Behaviors
{
    public class ValidationBehaviorTests
    {
        [Fact]
        public async Task Handle_WhenCommandIsValid_ShouldCallNext()
        {
            // Arrange
            var validatorMock = new Mock<IValidator<CreateTaskCommand>>();

            var validators = new List<IValidator<CreateTaskCommand>>
            {
                validatorMock.Object
            };

            var behavior = new ValidationBehavior<CreateTaskCommand, Guid>(validators);

            var command = new CreateTaskCommand(
                "Backup",
                "Description",
                "0 0 * * *",
                "backup.exe",
                3);

            var nextCalled = false;

            RequestHandlerDelegate<Guid> next = () =>
            {
                nextCalled = true;
                return Task.FromResult(Guid.NewGuid());
            };

            // Act
            await behavior.Handle(command, next, CancellationToken.None);

            // Assert
            nextCalled.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_WhenCommandIsInvalid_ShouldThrowValidationException()
        {
            // Arrange
            var validatorMock = new Mock<IValidator<CreateTaskCommand>>();

            var failures = new List<ValidationFailure>
            {
                new ValidationFailure("Name", "Name is required.")
            };

            validatorMock.Setup(x => x.ValidateAsync(It.IsAny<ValidationContext<CreateTaskCommand>>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync(new ValidationResult(failures));
            
            var validators = new List<IValidator<CreateTaskCommand>>
            {
                validatorMock.Object
            };

            var behavior = new ValidationBehavior<CreateTaskCommand, Guid>(validators);

            var command = new CreateTaskCommand(
                "",                 // invalid
                "Description",
                "0 0 * * *",
                "backup.exe",
                3);

            var nextCalled = false;

            RequestHandlerDelegate<Guid> next = () =>
            {
                nextCalled = true;
                return Task.FromResult(Guid.NewGuid());
            };

            // Act
            var action = async () => await behavior.Handle(command, next, CancellationToken.None);

            // Assert
            await action.Should().ThrowAsync<ValidationException>();

            nextCalled.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_WhenNoValidator_ShouldCallNext()
        {
            // Arrange
            var validators = new List<IValidator<CreateTaskCommand>>();


            var behavior = new ValidationBehavior<CreateTaskCommand, Guid>(validators);

            var command = new CreateTaskCommand(
                "",
                "",
                "",
                "",
                0);

            var nextCalled = false;

            RequestHandlerDelegate<Guid> next = () =>
            {
                nextCalled = true;
                return Task.FromResult(Guid.NewGuid());
            };

            // Act
            await behavior.Handle(command, next, CancellationToken.None);

            // Assert
            nextCalled.Should().BeTrue();
        }
    }
}