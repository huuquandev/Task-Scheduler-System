using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using TaskScheduler.Application.Interfaces;
using TaskScheduler.Domain.Entities;
using FluentValidation;
namespace TaskScheduler.Application.Tasks.Queries.GetTasks
{
    public class GetTasksHandler : IRequestHandler<GetTasksQuery, List<TaskSummaryDto>>
    {
        private readonly ITaskRepository _repo;

        public GetTasksHandler(ITaskRepository repo)
        {
            _repo = repo;
        }

        public async Task<List<TaskSummaryDto>> Handle(GetTasksQuery request, CancellationToken cancellationToken)
        {
            var tasks = await _repo.GetAllAsync();
            return tasks.Select(t => new TaskSummaryDto
            {
                Id = t.Id,
                Name = t.Name,
                Status = t.Status.ToString()
            }).ToList();
        }
    }
}