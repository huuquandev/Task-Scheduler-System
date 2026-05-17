using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskScheduler.Domain.Entities;

namespace TaskScheduler.Application.Interfaces
{
    public interface ITaskRepository
    {
        Task AddAsync(ScheduledTask task);
        Task<List<ScheduledTask>> GetAllAsync();   
        Task<ScheduledTask> GetByIdAsync(Guid id);
        Task UpdateAsync(ScheduledTask task);
    }
}