using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using TaskScheduler.Application.Interfaces;

namespace TaskScheduler.Application.Tasks.Commands.PauseTask
{
    public class PauseTaskHandler : IRequestHandler<PauseTaskCommand, Unit>
    {
        private readonly ITaskRepository _repo;
        private readonly IUnitOfWork _unitOfWork;

        private readonly ISchedulerService _scheduler;
        public PauseTaskHandler(ITaskRepository repo, IUnitOfWork unitOfWork, ISchedulerService scheduler)
        {
            _repo = repo;
            _unitOfWork = unitOfWork;
            _scheduler = scheduler;
        }
        public async Task<Unit> Handle(PauseTaskCommand request, CancellationToken cancellationToken)
        {
            var task = await _repo.GetByIdAsync(request.Id);

            if(task == null)
                throw new ArgumentException("Task not found.");

            if(task.IsDeleted)
                throw new InvalidOperationException("Task deleted");

            task.Pause();
            
            // Update DBContext
            await _repo.UpdateAsync(task);
            
            // Cancel the task in the scheduler if it's scheduled
            await _scheduler.UnscheduleTaskAsync(task.Id);

            // Save change to DB
            await _unitOfWork.SaveChangesAsync();

            return Unit.Value;
            
        }
    }
}