using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;

namespace TaskScheduler.Application.Tasks.Commands.DeleteTask
{
    public record DeleteTaskCommand(Guid Id) : IRequest<Unit>;
}