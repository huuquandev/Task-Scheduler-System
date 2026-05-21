using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;

namespace TaskScheduler.Application.Tasks.Commands.ResumeTask
{
    public record ResumeTaskCommand(Guid Id) : IRequest<Unit>;
}