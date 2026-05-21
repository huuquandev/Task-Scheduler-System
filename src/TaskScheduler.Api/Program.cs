using Microsoft.EntityFrameworkCore;
using MediatR;
using System.Reflection;
using FluentValidation;
using Hangfire;
using Hangfire.PostgreSql;
using TaskScheduler.Application.Interfaces;
using TaskScheduler.Infrastructure.Persistence;
using TaskScheduler.Infrastructure.Repositories;
using TaskScheduler.Infrastructure.Scheduling;
using TaskScheduler.Application.Common.Behaviors;
using TaskScheduler.Application.Tasks.Commands.CreateTask;
using TaskScheduler.Application.Tasks.Commands.UpdateTask;
using TaskScheduler.Application.Common.Mappings;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger with annotations
builder.Services.AddSwaggerGen(c =>
{
    c.EnableAnnotations();
});

// MediatR
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(
        typeof(TaskScheduler.Application.AssemblyReference).Assembly
    );
});

// DB
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// DI
builder.Services.AddScoped<ITaskRepository, TaskRepository>();
builder.Services.AddScoped<ITaskExecutionLogRepository, TaskExecutionLogRepository>();
builder.Services.AddScoped<ITaskExecutionService, TaskExecutionService>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateTaskCommandValidator>();
builder.Services.AddTransient(typeof(IPipelineBehavior<,>),typeof(ValidationBehavior<,>));
builder.Services.AddAutoMapper(typeof(TaskMappingProfile).Assembly);

// Hangfire
builder.Services.AddHangfire(config =>
{
    config.UsePostgreSqlStorage(options =>
    {
        options.UseNpgsqlConnection(builder.Configuration.GetConnectionString("DefaultConnection"));
    });
});
builder.Services.AddHangfireServer();
builder.Services.AddScoped<ISchedulerService, HangfireSchedulerService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

// Các middleware khác
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseRouting();
app.UseAuthorization();
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[]
    {
        new HangfireAuthorizationFilter()
    }
});

app.MapControllers();

app.Run();

