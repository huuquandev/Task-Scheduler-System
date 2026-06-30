using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using TaskScheduler.Application.Interfaces;
using TaskScheduler.Domain.Entities;
using TaskScheduler.Domain.Enums;

namespace TaskScheduler.Application.Tasks.Commands.TriggerTask
{
    public class TriggerTaskHandler : IRequestHandler<TriggerTaskCommand, Unit>
    {
        private readonly ITaskRepository _repo;
        private readonly ITaskExecutionService _executionService;
        public TriggerTaskHandler(ITaskRepository repo, ITaskExecutionService executionService)
        {
            _repo = repo;
            _executionService = executionService;
        }
        public async Task<Unit> Handle(TriggerTaskCommand request, CancellationToken cancellationToken)
        {
            var task = await _repo.GetByIdAsync(request.Id);

            if(task == null)
                throw new KeyNotFoundException("Task not found.");

            if(task.IsDeleted)
                throw new InvalidOperationException("Task deleted");

            if(task.Status != ScheduledTaskStatus.Active && task.Status != ScheduledTaskStatus.Failed)
            {
                throw new InvalidOperationException("Only Active or Failed tasks can be triggered manually.");
            }
            
            // Execute immediately
            await _executionService.TriggerNow(task.Id);

            return Unit.Value;
        }
    }
}