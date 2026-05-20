using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using TaskScheduler.Application.Interfaces;

namespace TaskScheduler.Application.Tasks.Queries.GetDetailsTaskExecutionLog
{
    public class GetDetailsTaskExecutionLogHandler : IRequestHandler<GetDetailsTaskExecutionLogQuery, TaskExecutionLogDetailsDto>
    {
        private readonly ITaskExecutionLogRepository _repo;
        private readonly IMapper _mapper;

        public GetDetailsTaskExecutionLogHandler(ITaskExecutionLogRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;   
        }

        public async Task<TaskExecutionLogDetailsDto> Handle(GetDetailsTaskExecutionLogQuery request, CancellationToken cancellationToken)
        {
            var log = await _repo.GetDetailsAsync(request.LogId);
            if (log == null || log.TaskId != request.TaskId)
                throw new KeyNotFoundException("Task not found");

            return _mapper.Map<TaskExecutionLogDetailsDto>(log);
        }
    }

}