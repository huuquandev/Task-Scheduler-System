using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using TaskScheduler.Application.Interfaces;
using TaskScheduler.Domain.Entities;

namespace TaskScheduler.Application.Tasks.Commands.TriggerTask
{
    public class TriggerTaskHandler : IRequestHandler<TriggerTaskCommand, Guid>
    {
        private readonly ITaskRepository _repo;
        private readonly ITaskExecutionLogRepository _logrepo;

        public TriggerTaskHandler(ITaskRepository repo, ITaskExecutionLogRepository logrepo)
        {
            _repo = repo;
            _logrepo = logrepo;
        }
        public async Task<Guid> Handle(TriggerTaskCommand request, CancellationToken cancellationToken)
        {
            var task = await _repo.GetByIdAsync(request.Id);

            if(task == null)
                throw new ArgumentException("Task not found.");

            if(task.IsDeleted)
                throw new InvalidOperationException("Task deleted");

            // Task start
            task.MarkAsRunning();
            var log = new TaskExecutionLog(task.Id);
            await _logrepo.AddAsync(log);

            try
            {
                // Execute task
                Console.WriteLine(task.Command);

                // SUCCESS
                task.MarkAsCompleted();

                log.MarkAsSuccess();
            }
            catch (Exception ex)
            {
                // FAILED
                task.MarkAsFailed(ex.Message);

                log.MarkAsFailed(ex.Message);
            }

            await _repo.UpdateAsync(task);

            await _logrepo.UpdateAsync(log);
            return task.Id;
        }
    }
}