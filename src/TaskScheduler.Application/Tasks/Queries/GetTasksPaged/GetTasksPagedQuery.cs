using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;

namespace TaskScheduler.Application.Tasks.Queries.GetTasksPaged
{
    public record GetTasksPagedQuery(int Page, int PageSize) : IRequest<PagedResult<TaskDto>>;
}