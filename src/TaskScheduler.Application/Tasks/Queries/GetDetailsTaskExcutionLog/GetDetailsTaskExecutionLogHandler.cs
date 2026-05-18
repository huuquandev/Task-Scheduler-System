using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;

namespace TaskScheduler.Application.Tasks.Queries.GetDetailsTaskExcutionLog
{
    public interface GetDetailsTaskExecutionLogHandler : IRequestHandler<GetDetailsTaskExecutionLogQuery, ExecutionLogDetailsDto>
    {
        private readonly ITaskExecutionLogRepository _repo;
        private readonly IMapper _mapper;

        public GetDetailsTaskExecutionLogHandler(ITaskExecutionLogRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;   
        }

        public async Task<ExecutionLogDetailsDto> Handle(GetDetailsTaskExecutionLogQuery request, CancellationToken cancellationToken)
        {
            var log = await _repo.GetByIdAsync(request.LogId);
            if (log == null || log.TaskId != request.TaskId)
                throw new NotFoundException("Execution log not found for the specified task.");

            return _mapper.Map<ExecutionLogDetailsDto>(log);
        }
    }

}