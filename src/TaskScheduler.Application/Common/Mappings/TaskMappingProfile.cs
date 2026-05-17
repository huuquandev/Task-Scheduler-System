using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using TaskScheduler.Application.Tasks.Queries.GetTaskById;
using TaskScheduler.Application.Tasks.Queries.GetTaskExecutionLogs;
using TaskScheduler.Application.Tasks.Queries.GetTasks;
using TaskScheduler.Domain.Entities;

namespace TaskScheduler.Application.Common.Mappings
{
    public class TaskMappingProfile : Profile
    {
         public TaskMappingProfile()
        {
            CreateMap<ScheduledTask, TaskSummaryDto>();

            CreateMap<ScheduledTask, TaskDto>();
            CreateMap<TaskExecutionLog, ExecutionLogDto>();
        }
    }
}