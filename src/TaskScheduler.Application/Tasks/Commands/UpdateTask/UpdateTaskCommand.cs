using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;

namespace TaskScheduler.Application.Tasks.Commands.UpdateTask
{
    public record UpdateTaskCommand(
        Guid Id,
        string? Name = null,
        string? Description = null,
        string? CronExpression = null,
        string? Command = null,
        int? MaxRetries = null
    ) : IRequest<Guid>;
}