using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using TaskScheduler.Application.Interfaces;
using TaskScheduler.Domain.Entities;

namespace TaskScheduler.Application.Tasks.Queries.GetTasks
{
    public class GetTasksHandler : IRequestHandler<GetTasksQuery, List<ScheduledTask>>
    {
        private readonly ITaskRepository _repo;

        public GetTasksHandler(ITaskRepository repo)
        {
            _repo = repo;
        }

        public async Task<List<ScheduledTask>> Handle(GetTasksQuery request, CancellationToken cancellationToken)
        {
            return await _repo.ListAllAsync();
        }
    }
}