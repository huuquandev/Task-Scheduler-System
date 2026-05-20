using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;

namespace TaskScheduler.Application.Tasks.Commands.ActiveTask
{
    public record ActiveTaskCommand(Guid Id) : IRequest<Guid>;
}