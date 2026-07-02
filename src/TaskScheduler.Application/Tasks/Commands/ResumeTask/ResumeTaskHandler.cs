using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using TaskScheduler.Application.Interfaces;

namespace TaskScheduler.Application.Tasks.Commands.ResumeTask
{
    public class ResumeTaskHandler : IRequestHandler<ResumeTaskCommand, Unit>
    {
        private readonly ITaskRepository _repo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISchedulerService _scheduler;
        public ResumeTaskHandler(ITaskRepository repo, IUnitOfWork unitOfWork, ISchedulerService scheduler)
        {
            _repo = repo;
            _unitOfWork = unitOfWork;
            _scheduler = scheduler;
        }
        
        public async Task<Unit> Handle(ResumeTaskCommand request, CancellationToken cancellationToken)
        {
            var task = await _repo.GetByIdAsync(request.Id);

            if(task == null)
                throw new ArgumentException("Task not found.");
            if(task.IsDeleted)
                throw new InvalidOperationException("Task deleted");  
                
            if(task.Status != Domain.Enums.ScheduledTaskStatus.Paused)
                throw new InvalidOperationException("Only paused tasks can be resumed.");
                
            task.MarkAsActive();

            // Update DBContext
            await _repo.UpdateAsync(task);

            // execution
            await _scheduler.RescheduleTaskAsync(task);

            // Save change to DB
            await _unitOfWork.SaveChangesAsync();

            return Unit.Value;
        }
    }
}