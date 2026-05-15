using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

            if(string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("Task is required.");

            if(string.IsNullOrWhiteSpace(request.Command))
                throw new ArgumentException("Command is required.");

            if (request.MaxRetries < 0 || request.MaxRetries > 10)
                throw new ArgumentException("MaxRetries invalid");
            
            var existing = await _repo.GetByNameAsync(request.Name);   
            if (existing != null && existing.Id != request.Id)
                throw new ArgumentException("Task name already exists");
            
            task.Update(
                request.Name,
                request.Description,
                request.CronExpression,
                request.Command,
                request.MaxRetries
            );

            await _repo.UpdateAsync(task);

            return task.Id;
        }
    }
}