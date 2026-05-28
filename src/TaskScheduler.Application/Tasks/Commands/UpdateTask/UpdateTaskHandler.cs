using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using TaskScheduler.Application.Interfaces;
using TaskScheduler.Domain.Entities;
using TaskScheduler.Domain.ValueObjects;
namespace TaskScheduler.Application.Tasks.Commands.UpdateTask
{
    public class UpdateTaskHandler : IRequestHandler<UpdateTaskCommand, Unit>
    {
        private readonly ITaskRepository _repo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISchedulerService _scheduler;

        public UpdateTaskHandler(ITaskRepository repo, IUnitOfWork unitOfWork, ISchedulerService scheduler)
        {
            _repo = repo;
            _unitOfWork = unitOfWork;
            _scheduler = scheduler;
        }
        public async Task<Unit> Handle(UpdateTaskCommand request, CancellationToken cancellationToken)
        {
            var task = await _repo.GetByIdAsync(request.Id);
            if (task == null)
                throw new ArgumentException("Task not found.");
                
            if(task.IsDeleted)
                throw new InvalidOperationException("Task deleted");

            var cron = request.CronExpression != null ? CronExpression.Create(request.CronExpression): task.CronExpression;
            task.Update(
                request.Name ?? task.Name,
                request.Description ?? task.Description,
                cron,
                request.Command ?? task.Command,
                request.MaxRetries ?? task.MaxRetries
            );

            // Update DBContext
            await _repo.UpdateAsync(task);
            
            // Reschedule the task in the scheduler
            await _scheduler.RescheduleTaskAsync(task);

            // Save change to DB
            await _unitOfWork.SaveChangesAsync();

            return Unit.Value;
        }
    }
}