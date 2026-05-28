using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using MediatR;
using TaskScheduler.Application.Interfaces;


namespace TaskScheduler.Application.Tasks.Commands.DeleteTask
{
    public class DeleteTaskHandler : IRequestHandler<DeleteTaskCommand, Unit>
    {
        private readonly ITaskRepository _repo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISchedulerService _scheduler;
        public DeleteTaskHandler(ITaskRepository repo, IUnitOfWork unitOfWork, ISchedulerService scheduler)
        {
            _repo = repo;
            _unitOfWork = unitOfWork;
            _scheduler = scheduler;
        }
        public async Task<Unit> Handle(DeleteTaskCommand request, CancellationToken cancellationToken)
        {
            var task = await _repo.GetByIdAsync(request.Id);
            if (task == null)
                throw new ArgumentException("Task not found.");

            if(task.IsDeleted)
                throw new InvalidOperationException("Task deleted");
                
            task.SoftDelete();

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