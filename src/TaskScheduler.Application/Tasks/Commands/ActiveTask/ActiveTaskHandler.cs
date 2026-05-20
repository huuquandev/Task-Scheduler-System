using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using TaskScheduler.Application.Interfaces;

namespace TaskScheduler.Application.Tasks.Commands.ActiveTask
{
    public class ActiveTaskHandler : IRequestHandler<ActiveTaskCommand, Guid>
    {
        private readonly ITaskRepository _repo;
        private readonly ISchedulerService _scheduler;
        public ActiveTaskHandler(ITaskRepository repo, ISchedulerService scheduler)
        {
            _repo = repo;
            _scheduler = scheduler;
        }

        public async Task<Guid> Handle(ActiveTaskCommand request, CancellationToken cancellationToken)
        {
            var task = await _repo.GetByIdAsync(request.Id);

            if(task == null)
                throw new ArgumentException("Task not found.");
            if(task.IsDeleted)
                throw new InvalidOperationException("Task deleted");

            if(task.Status != Domain.Enums.ScheduledTaskStatus.Pending)
                throw new InvalidOperationException("Only pending tasks can be activated.");

            task.Activate();
            await _repo.UpdateAsync(task);
            // execution
            await _scheduler.ScheduleTaskAsync(task);

            return task.Id;
        }
    }
}