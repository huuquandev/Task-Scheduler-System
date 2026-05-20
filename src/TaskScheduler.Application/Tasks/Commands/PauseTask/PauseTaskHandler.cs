using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using TaskScheduler.Application.Interfaces;

namespace TaskScheduler.Application.Tasks.Commands.PauseTask
{
    public class PauseTaskHandler : IRequestHandler<PauseTaskCommand, Guid>
    {
        private readonly ITaskRepository _repo;
        private readonly ISchedulerService _scheduler;
        public PauseTaskHandler(ITaskRepository repo, ISchedulerService scheduler)
        {
            _repo = repo;
            _scheduler = scheduler;
        }
        public async Task<Guid> Handle(PauseTaskCommand request, CancellationToken cancellationToken)
        {
            var task = await _repo.GetByIdAsync(request.Id);

            if(task == null)
                throw new ArgumentException("Task not found.");

            if(task.IsDeleted)
                throw new InvalidOperationException("Task deleted");

            task.Pause();
            await _repo.UpdateAsync(task);

            // Cancel the task in the scheduler if it's scheduled
            await _scheduler.UnscheduleTaskAsync(task.Id);

            return task.Id;
            
        }
    }
}