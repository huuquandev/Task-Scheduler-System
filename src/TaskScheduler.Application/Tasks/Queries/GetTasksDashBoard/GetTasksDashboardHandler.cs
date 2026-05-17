using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using TaskScheduler.Application.Interfaces;
using TaskScheduler.Domain.Entities;
using TaskScheduler.Domain.Enums;

namespace TaskScheduler.Application.Tasks.Queries.GetTasksDashBoard
{
    public class GetTasksDashboardHandler : IRequestHandler<GetTasksDashboardQuery, TasksDashboardDto>
    {
        private readonly ITaskRepository _repo;
        private readonly ITaskExecutionLogRepository _logrepo;

        public GetTasksDashboardHandler(ITaskRepository repo, ITaskExecutionLogRepository logrepo)
        {
            _repo = repo;
            _logrepo = logrepo;
        }

        public async Task<TasksDashboardDto> Handle(GetTasksDashboardQuery request, CancellationToken cancellationToken)
        {
            var tasks = await _repo.GetAllAsync();
            var logs = await _logrepo.GetAllAsync();
            return new TasksDashboardDto
            {
                TotalTasks = tasks.Count,

                ActiveTasks = tasks.Count(x => x.Status == ScheduledTaskStatus.Active),

                PausedTasks = tasks.Count(x => x.Status == ScheduledTaskStatus.Paused),

                DeletedTasks = tasks.Count(x => x.IsDeleted),

                RunningTasks = logs.Count(x => x.Status == TaskExecutionStatus.Running),

                FailedExecutions = logs.Count(x => x.Status == TaskExecutionStatus.Failed),

                SuccessExecutions = logs.Count(x => x.Status == TaskExecutionStatus.Success)
            };
            
        }
    }
}