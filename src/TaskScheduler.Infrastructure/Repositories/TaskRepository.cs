using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TaskScheduler.Application.Interfaces;
using TaskScheduler.Domain.Entities;
using TaskScheduler.Infrastructure.Persistence;

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
            await _context.SaveChangesAsync();
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
            await _context.SaveChangesAsync();
        }
    }
}