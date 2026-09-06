using Microsoft.EntityFrameworkCore;
using Serilog;
using TaskScheduler.Api.Extensions;
using TaskScheduler.Api.Middleware;
using TaskScheduler.Application;
using TaskScheduler.Infrastructure;
using TaskScheduler.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog(
    (ctx, config) =>
    {
        config.ReadFrom.Configuration(ctx.Configuration);
    }
);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();


// DI
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);
builder.Services.AddApplication();
builder.Services.AddSwaggerConfiguration();
builder.Services.AddAuthenticationConfiguration(builder.Configuration);
builder.Services.AddTelemetry();
builder.Services.AddCorsConfiguration();
builder.Services.AddRateLimiting();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrEmpty(connectionString))
{
    builder.Services.AddHealthChecks()
        .AddNpgSql(connectionString);
}
else
{
    builder.Services.AddHealthChecks();
}

var app = builder.Build();

app.UseHangfireDashboardConfiguration(app.Environment);

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseSerilogRequestLogging();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();
app.MapHealthChecks("/health");
if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();

public partial class Program
{
}