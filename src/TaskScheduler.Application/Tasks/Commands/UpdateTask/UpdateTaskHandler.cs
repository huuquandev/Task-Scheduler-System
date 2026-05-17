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
    public class UpdateTaskHandler : IRequestHandler<UpdateTaskCommand, Guid>
    {
        private readonly ITaskRepository _repo;

        public UpdateTaskHandler(ITaskRepository repo)
        {
            _repo = repo;
        }
        public async Task<Guid> Handle(UpdateTaskCommand request, CancellationToken cancellationToken)
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

            await _repo.UpdateAsync(task);

            return task.Id;
        }
    }
}