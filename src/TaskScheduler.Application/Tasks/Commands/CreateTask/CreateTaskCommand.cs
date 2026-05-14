using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;

namespace TaskScheduler.Application.Tasks.Commands.CreateTask
{
    public record CreateTaskCommand(string Name, string CronExpression) : IRequest<Guid>;

}