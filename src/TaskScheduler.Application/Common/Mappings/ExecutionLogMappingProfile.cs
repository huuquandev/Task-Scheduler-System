using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using TaskScheduler.Application.Tasks.Queries.GetTaskExecutionLogs;
using TaskScheduler.Application.Tasks.Queries.GetDetailsTaskExcutionLog;
using TaskScheduler.Domain.Entities;

namespace TaskScheduler.Application.Common.Mappings
{
    public class ExecutionLogMappingProfile : Profile
    {
        public ExecutionLogMappingProfile()
        {
            CreateMap<TaskExecutionLog, ExecutionLogDto>();
            CreateMap<TaskExecutionLog, ExecutionLogDetailsDto>()
                .ForMember(dest => dest.TaskName, opt => opt.MapFrom(src => src.ScheduledTask.Name))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));;

        }
    }
}