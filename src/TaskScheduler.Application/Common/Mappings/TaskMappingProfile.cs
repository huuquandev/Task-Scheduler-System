using AutoMapper;
using TaskScheduler.Application.Tasks.Queries.GetTaskById;
using TaskScheduler.Application.Tasks.Queries.GetTasks;
using TaskScheduler.Domain.Entities;

namespace TaskScheduler.Application.Common.Mappings
{
    public class TaskMappingProfile : Profile
    {
        public TaskMappingProfile()
        {
            CreateMap<ScheduledTask, TaskSummaryDto>()
                .ForMember(dest => dest.Status,
                    opt => opt.MapFrom(src => src.Status.ToString()));

            CreateMap<ScheduledTask, TaskDto>()
                .ForMember(dest => dest.Status,
                    opt => opt.MapFrom(src => src.Status.ToString()))
                .ForMember(dest => dest.CronExpression,
                    opt => opt.MapFrom(src => src.CronExpression.Value));
        }
    }
}