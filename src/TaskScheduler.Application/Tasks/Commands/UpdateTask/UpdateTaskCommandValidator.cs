using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;

namespace TaskScheduler.Application.Tasks.Commands.UpdateTask
{
    public class UpdateTaskCommandValidator : AbstractValidator<UpdateTaskCommand>
    {
        public UpdateTaskCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .When(x => x.Name != null);

            RuleFor(x => x.Command)
                .NotEmpty()
                .When(x => x.Command != null);

            RuleFor(x => x.CronExpression)
                .NotEmpty()
                .When(x => x.CronExpression != null);

            RuleFor(x => x.MaxRetries)
                .InclusiveBetween(0, 10)
                .When(x => x.MaxRetries != null);
        }    
    }
}