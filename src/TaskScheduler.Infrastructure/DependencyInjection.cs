using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TaskScheduler.Application.Interfaces;
using TaskScheduler.Infrastructure.Persistence;
using TaskScheduler.Infrastructure.Repositories;
using TaskScheduler.Infrastructure.Scheduling;
using TaskScheduler.Infrastructure.Services;
using Hangfire;
using Hangfire.PostgreSql;
namespace TaskScheduler.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
        {
            if (!environment.IsEnvironment("Testing"))
            {
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));
                });
                services.AddHangfire(config =>
                {
                    config.UsePostgreSqlStorage(options =>
                    {
                        options.UseNpgsqlConnection(configuration.GetConnectionString("DefaultConnection"));
                    });
                });

                services.AddHangfireServer();
                services.AddScoped<ISchedulerService, HangfireSchedulerService>();
            }

            services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ApplicationDbContext>());

            services.AddScoped<ITaskRepository, TaskRepository>();
            services.AddScoped<ITaskExecutionLogRepository, TaskExecutionLogRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<ITaskExecutionService, TaskExecutionService>();
            services.AddScoped<IMetricsService, MetricsService>();

            return services;
        }
    }
}