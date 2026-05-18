using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;

namespace TaskScheduler.Application.Tasks.Queries.GetDetailsTaskExcutionLog
{
    public record GetDetailsTaskExecutionLogQuery(Guid TaskId, Guid LogId) : IRequest<ExecutionLogDetailsDto>;
}