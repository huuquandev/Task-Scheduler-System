using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MediatR;
using FluentValidation;
using TaskScheduler.Application.Tasks.Commands.CreateTask;
using TaskScheduler.Application.Common.Behaviors;
using TaskScheduler.Application.Common.Mappings;
namespace TaskScheduler.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(
                    typeof(TaskScheduler.Application.AssemblyReference).Assembly
                );
            });
            services.AddValidatorsFromAssemblyContaining<CreateTaskCommandValidator>();
            services.AddAutoMapper(typeof(TaskMappingProfile).Assembly);
            services.AddTransient(typeof(IPipelineBehavior<,>),typeof(LoggingBehavior<,>));
            services.AddTransient(typeof(IPipelineBehavior<,>),typeof(ValidationBehavior<,>));

            return services;
        }
    }
}