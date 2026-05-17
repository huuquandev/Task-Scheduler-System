using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using MediatR;
using TaskScheduler.Application.Interfaces;

namespace TaskScheduler.Application.Tasks.Commands.DeleteTask
{
    public class DeleteTaskHandler : IRequestHandler<DeleteTaskCommand, Guid>
    {
        private readonly ITaskRepository _repo;
        public DeleteTaskHandler(ITaskRepository repo)
        {
            _repo = repo;
        }
        public async Task<Guid> Handle(DeleteTaskCommand request, CancellationToken cancellationToken)
        {
            var task = await _repo.GetByIdAsync(request.Id);
            if (task == null)
                throw new ArgumentException("Task not found.");

            if(task.IsDeleted)
                throw new InvalidOperationException("Task deleted");
                
            task.SoftDelete();
            await _repo.UpdateAsync(task);

            return task.Id;
        }
    }
}