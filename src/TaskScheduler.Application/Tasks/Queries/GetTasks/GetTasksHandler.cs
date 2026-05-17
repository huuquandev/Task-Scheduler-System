using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using TaskScheduler.Application.Interfaces;

namespace TaskScheduler.Application.Tasks.Queries.GetTasks
{
    public class GetTasksHandler : IRequestHandler<GetTasksQuery, List<TaskSummaryDto>>
    {
        private readonly ITaskRepository _repo;
        private readonly IMapper _mapper;

        public GetTasksHandler(ITaskRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        public async Task<List<TaskSummaryDto>> Handle(GetTasksQuery request, CancellationToken cancellationToken)
        {
            var tasks = await _repo.GetAllAsync();
            return _mapper.Map<List<TaskSummaryDto>>(tasks);
        }
    }
}