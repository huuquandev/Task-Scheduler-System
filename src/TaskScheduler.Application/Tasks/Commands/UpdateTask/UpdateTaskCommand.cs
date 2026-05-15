using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TaskScheduler.Application.Tasks.Commands.UpdateTask
{
    public record UpdateTaskCommand(
        Guid Id,
        string Name,
        string Description,
        string CronExpression,
        string Command,
        int MaxRetries
    ) : IRequest<Guid>;
}