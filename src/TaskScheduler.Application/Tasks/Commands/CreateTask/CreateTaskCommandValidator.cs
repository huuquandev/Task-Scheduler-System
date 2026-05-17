using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;

namespace TaskScheduler.Application.Tasks.Commands.CreateTask
{
    public class CreateTaskCommandValidator : AbstractValidator<CreateTaskCommand>
    {
        public CreateTaskCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Task name is required.")
                .MaximumLength(100)
                .WithMessage("Task name must not exceed 100 characters.");

            RuleFor(x => x.Command)
                .NotEmpty()
                .WithMessage("Command is required.");

            RuleFor(x => x.CronExpression)
                .NotEmpty()
                .WithMessage("CronExpression is required.");

            RuleFor(x => x.MaxRetries)
                .InclusiveBetween(0, 10)
                .WithMessage("MaxRetries must be between 0 and 10.");
        }
    }
}