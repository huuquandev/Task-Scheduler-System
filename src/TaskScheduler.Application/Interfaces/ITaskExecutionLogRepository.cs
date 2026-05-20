using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskScheduler.Domain.Entities;

namespace TaskScheduler.Application.Interfaces
{
    public interface ITaskExecutionLogRepository
    {
        Task AddAsync(TaskExecutionLog task);
        Task UpdateAsync(TaskExecutionLog task);
        Task<List<TaskExecutionLog>> GetByTaskIdAsync(Guid id);
        Task<List<TaskExecutionLog>> GetAllAsync();
        Task<TaskExecutionLog> GetDetailsAsync(Guid id);
    }
}