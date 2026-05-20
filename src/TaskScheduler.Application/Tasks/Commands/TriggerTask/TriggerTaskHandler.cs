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
        private readonly ISchedulerService _scheduler;
        public TriggerTaskHandler(ITaskRepository repo, ISchedulerService scheduler)
        {
            _repo = repo;
            _scheduler = scheduler;
        }
        public async Task<Guid> Handle(TriggerTaskCommand request, CancellationToken cancellationToken)
        {
            var task = await _repo.GetByIdAsync(request.Id);

            if(task == null)
                throw new ArgumentException("Task not found.");

            if(task.IsDeleted)
                throw new InvalidOperationException("Task deleted");

            await _executionService.ExecuteTask(task.Id);

            return task.Id;
        }
    }
}