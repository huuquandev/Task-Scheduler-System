using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using TaskScheduler.Domain.Entities;

namespace TaskScheduler.Application.Tasks.Queries.GetTasks
{
    public record GetTasksQuery() : IRequest<List<ScheduledTask>>;
}