using Microsoft.EntityFrameworkCore;
using TaskScheduler.Application.Interfaces;
using TaskScheduler.Infrastructure.Persistence;
using TaskScheduler.Infrastructure.Repositories;
using MediatR;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

// DI (ONLY repositories)
builder.Services.AddScoped<ITaskRepository, TaskRepository>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();