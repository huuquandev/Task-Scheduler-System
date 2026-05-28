using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using TaskScheduler.Application.Interfaces;
using TaskScheduler.Domain.Entities;

namespace TaskScheduler.Application.Tasks.Commands.CreateTask
{
    public class CreateTaskHandler : IRequestHandler<CreateTaskCommand, Guid>
    {
        private readonly ITaskRepository _repo;
        private readonly IUnitOfWork _unitOfWork;
        public CreateTaskHandler(ITaskRepository repo, IUnitOfWork unitOfWork)
        {
            _repo = repo;
            _unitOfWork = unitOfWork;
        }

        public async Task<Guid> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
        {   
            var task = new ScheduledTask(
                request.Name,
                request.Description,
                request.CronExpression,
                request.Command,
                request.MaxRetries
            );
            
            task.UpdateNextRunTime();
            // Add DBContext
            await _repo.AddAsync(task);

            // Save change to DB
            await _unitOfWork.SaveChangesAsync();

            return task.Id;

        }
    }
}