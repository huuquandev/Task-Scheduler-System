using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using TaskScheduler.Application.Interfaces;

namespace TaskScheduler.Application.Tasks.Queries.GetTaskExecutionLogs
{
    public class GetTaskExecutionLogsHandler : IRequestHandler<GetTaskExecutionLogsQuery, List<ExecutionLogDto>>
    {
        private readonly ITaskExecutionLogRepository _repo;
        private readonly IMapper _mapper;

        public GetTaskExecutionLogsHandler(ITaskExecutionLogRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;   
        }

        public async Task<List<ExecutionLogDto>> Handle(GetTaskExecutionLogsQuery request, CancellationToken cancellationToken)
        {
            var logs = await _repo.GetByTaskIdAsync(request.Id);
            return _mapper.Map<List<ExecutionLogDto>>(logs);
        }   
        
    }
}