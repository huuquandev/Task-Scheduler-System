using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using TaskScheduler.Application.Interfaces;
using TaskScheduler.Domain.Entities;
using TaskScheduler.Domain.Enums;
using TaskScheduler.Application.Common.Models;
using TaskScheduler.Application.Tasks.Queries.GetTaskById;

namespace TaskScheduler.Application.Tasks.Queries.GetTasksPaged
{
    public class GetTasksPagedHandler : IRequestHandler<GetTasksPagedQuery, PagedResult<TaskDto>>
    {
        private readonly ITaskRepository _repo;
        private readonly IMapper _mapper;

        public GetTasksPagedHandler(ITaskRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        public async Task<PagedResult<TaskDto>> Handle(GetTasksPagedQuery request, CancellationToken cancellationToken)
        {
            var pagedTasks = await _repo.GetPagedAsync(request.Page, request.PageSize);
            var taskDtos = _mapper.Map<List<TaskDto>>(pagedTasks.Items);
            return new PagedResult<TaskDto>(taskDtos, pagedTasks.TotalCount, request.Page, request.PageSize);
        }
    }
}