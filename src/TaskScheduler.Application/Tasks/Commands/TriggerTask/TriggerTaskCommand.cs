using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;

namespace TaskScheduler.Application.Tasks.Commands.TriggerTask
{
    public record TriggerTaskCommand(Guid Id) : IRequest<Unit>;
}