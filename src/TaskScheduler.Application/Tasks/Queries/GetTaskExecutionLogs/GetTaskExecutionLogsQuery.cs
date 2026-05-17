using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using TaskScheduler.Domain.Entities;

namespace TaskScheduler.Application.Tasks.Queries.GetTaskExecutionLogs
{
    public record GetTaskExecutionLogsQuery(Guid Id) : IRequest<List<ExecutionLogDto>>;
}