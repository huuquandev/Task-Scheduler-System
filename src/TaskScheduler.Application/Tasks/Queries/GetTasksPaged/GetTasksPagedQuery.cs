using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using TaskScheduler.Application.Common.Models;
using TaskScheduler.Application.Tasks.Queries.GetTaskById;
using TaskScheduler.Domain.Enums;

namespace TaskScheduler.Application.Tasks.Queries.GetTasksPaged
{
    public record GetTasksPagedQuery(int Page, int PageSize, ScheduledTaskStatus? Status) : IRequest<PagedResult<TaskDto>>;
}