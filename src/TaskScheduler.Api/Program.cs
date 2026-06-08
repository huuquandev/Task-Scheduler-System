using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TaskScheduler.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using MediatR;
using System.Reflection;
using Microsoft.OpenApi;
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
using TaskScheduler.Api.Middleware;
using TaskScheduler.Api.Authorization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger with annotations
builder.Services.AddSwaggerGen(options =>
{
    options.EnableAnnotations();

    options.AddSecurityDefinition("bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "JWT Authorization header using the Bearer scheme."
    });

    options.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("bearer", document)] = []
        });
});

// MediatR
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(
        typeof(TaskScheduler.Application.AssemblyReference).Assembly
    );
});

// DB
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
}

// DI
builder.Services.AddScoped<ITaskRepository, TaskRepository>();
builder.Services.AddScoped<ITaskExecutionLogRepository, TaskExecutionLogRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<ITaskExecutionService, TaskExecutionService>();
builder.Services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ApplicationDbContext>());
builder.Services.AddValidatorsFromAssemblyContaining<CreateTaskCommandValidator>();
builder.Services.AddTransient(typeof(IPipelineBehavior<,>),typeof(ValidationBehavior<,>));
builder.Services.AddAutoMapper(typeof(TaskMappingProfile).Assembly);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
      .AddJwtBearer(options =>
      {
          options.TokenValidationParameters = new TokenValidationParameters
          {
              ValidateIssuer = true,
              ValidateAudience = true,
              ValidateLifetime = true,
              ValidateIssuerSigningKey = true,
              ValidIssuer = builder.Configuration["Jwt:Issuer"],
              ValidAudience = builder.Configuration["Jwt:Audience"],
              IssuerSigningKey = new SymmetricSecurityKey(
                  Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!))
          };
      });
     

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

// Hangfire
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddHangfire(config =>
    {
        config.UsePostgreSqlStorage(options =>
        {
            options.UseNpgsqlConnection(
                builder.Configuration.GetConnectionString("DefaultConnection"));
        });
    });

    builder.Services.AddHangfireServer();
    builder.Services.AddScoped<ISchedulerService, HangfireSchedulerService>();

}

var app = builder.Build();

if (!builder.Environment.IsEnvironment("Testing"))
{
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new[]
        {
            new HangfireAuthorizationFilter()
        }
    });
}

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
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program
{
}