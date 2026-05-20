using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskScheduler.Domain.Entities;
namespace TaskScheduler.Application.Interfaces
{
    public interface ISchedulerService
    {
        Task ScheduleTaskAsync(ScheduledTask task);       // đăng ký recurring job
        Task UnscheduleTaskAsync(Guid taskId);            // hủy job khi pause/delete
        Task RescheduleTaskAsync(ScheduledTask task);     // cập nhật khi update cron
    }
}