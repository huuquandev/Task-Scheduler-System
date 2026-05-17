    using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using TaskScheduler.Application.Interfaces;

namespace TaskScheduler.Application.Tasks.Commands.ResumeTask
{
    public class ResumeTaskHandler : IRequestHandler<ResumeTaskCommand, Guid>
    {
        private readonly ITaskRepository _repo;
        public ResumeTaskHandler(ITaskRepository repo)
        {
            _repo = repo;
        }
        
        public async Task<Guid> Handle(ResumeTaskCommand request, CancellationToken cancellationToken)
        {
            var task = await _repo.GetByIdAsync(request.Id);

            if(task == null)
                throw new ArgumentException("Task not found.");

            if(task.IsDeleted)
                throw new InvalidOperationException("Task deleted");  

            task.Activate();
            await _repo.UpdateAsync(task);

            return task.Id;
        }
    }
}