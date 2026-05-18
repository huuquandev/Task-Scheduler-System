using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TaskScheduler.Application.Interfaces;
using TaskScheduler.Domain.Entities;
using TaskScheduler.Infrastructure.Persistence;

namespace TaskScheduler.Infrastructure.Repositories
{
    public class TaskExecutionLogRepository : ITaskExecutionLogRepository
    {
        private readonly ApplicationDbContext _context;

        public TaskExecutionLogRepository(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task AddAsync(TaskExecutionLog task)
        {
            await _context.TaskExecutionLogs.AddAsync(task);
            await _context.SaveChangesAsync();
        }
        public async Task UpdateAsync(TaskExecutionLog task)
        {
            _context.TaskExecutionLogs.Update(task);
            await _context.SaveChangesAsync();
        }
        public async Task<List<TaskExecutionLog>> GetByTaskIdAsync(Guid id)
        {
            return await _context.TaskExecutionLogs.Include(x => x.ScheduledTask).Where(x => x.TaskId == id).ToListAsync();

        }
        public async Task<List<TaskExecutionLog>> GetAllAsync()
        {
            return await _context.TaskExecutionLogs.ToListAsync();
        }
        public async Task<TaskExecutionLog> GetDetailsAsync(Guid id)
        {
            return await _context.TaskExecutionLogs.Include(x => x.ScheduledTask).FirstOrDefaultAsync(x => x.Id == id);
        }
    }
}