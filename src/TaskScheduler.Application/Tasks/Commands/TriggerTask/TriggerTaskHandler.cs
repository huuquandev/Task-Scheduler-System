using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using TaskScheduler.Application.Interfaces;
using TaskScheduler.Domain.Entities;

namespace TaskScheduler.Application.Tasks.Commands.TriggerTask
{
    public class TriggerTaskHandler : IRequestHandler<TriggerTaskCommand, Guid>
    {
        private readonly ITaskRepository _repo;
        private readonly ITaskExecutionLogRepository _logrepo;
        private readonly ISchedulerService _scheduler;
        public TriggerTaskHandler(ITaskRepository repo, ITaskExecutionLogRepository logrepo, ISchedulerService scheduler)
        {
            _repo = repo;
            _logrepo = logrepo;
            _scheduler = scheduler;
        }
        public async Task<Guid> Handle(TriggerTaskCommand request, CancellationToken cancellationToken)
        {
            var task = await _repo.GetByIdAsync(request.Id);

            if(task == null)
                throw new ArgumentException("Task not found.");

            if(task.IsDeleted)
                throw new InvalidOperationException("Task deleted");

            await _repo.UpdateAsync(task);
            // execution
            await _scheduler.ScheduleTaskAsync(task);

            return task.Id;
        }
    }
}