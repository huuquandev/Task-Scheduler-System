using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TaskScheduler.Application.Interfaces;
using TaskScheduler.Domain.Entities;
using TaskScheduler.Infrastructure.Persistence;
using TaskScheduler.Application.Common.Models;
using TaskScheduler.Domain.Enums;

namespace TaskScheduler.Infrastructure.Repositories
{
    public class TaskRepository : ITaskRepository
    {
        private readonly ApplicationDbContext _context;

        public TaskRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(ScheduledTask task)
        {
            await _context.ScheduledTasks.AddAsync(task);
        }

        public async Task<List<ScheduledTask>> GetAllAsync()
        {
            return await _context.ScheduledTasks.Where(x => !x.IsDeleted).ToListAsync();
        }

        public async Task<ScheduledTask> GetByIdAsync(Guid id)
        {
            return await _context.ScheduledTasks.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        }

        public async Task UpdateAsync(ScheduledTask task)
        {
            _context.ScheduledTasks.Update(task);
        }
        public async Task<PagedResult<ScheduledTask>> GetPagedAsync(int page, int pageSize, ScheduledTaskStatus? status)
        {
            var query = _context.ScheduledTasks
                    .Where(x => !x.IsDeleted)
                    .Where(x => !status.HasValue || x.Status == status.Value);

            var totalCount = await query.CountAsync();

            var items = await query
                .Where(s => !status.HasValue || s.Status == status.Value)
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<ScheduledTask>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
    }
}