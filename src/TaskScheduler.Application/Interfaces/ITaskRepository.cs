using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskScheduler.Application.Common.Models;
using TaskScheduler.Domain.Entities;
using TaskScheduler.Domain.Enums;

namespace TaskScheduler.Application.Interfaces
{
    public interface ITaskRepository
    {
        Task AddAsync(ScheduledTask task);
        Task<List<ScheduledTask>> GetAllAsync();   
        Task<ScheduledTask?> GetByIdAsync(Guid id);
        Task UpdateAsync(ScheduledTask task);
        Task<PagedResult<ScheduledTask>> GetPagedAsync(int page, int pageSize, ScheduledTaskStatus? status);
    }
}