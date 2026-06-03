using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskScheduler.Application.Interfaces;
using TaskScheduler.Infrastructure.Persistence;
using Moq;

namespace TaskScheduler.Api.Tests
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
         private SqliteConnection? _connection;

        public Mock<ISchedulerService> SchedulerServiceMock { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services =>
            {
                // Remove DbContext current (PostgreSQL)
                var dbContextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

                if (dbContextDescriptor != null)
                {
                    services.Remove(dbContextDescriptor);
                }

                // SQLite InMemory

                _connection = new SqliteConnection("DataSource=:memory:");

                _connection.Open();

                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseSqlite(_connection);
                });

                // Remove Hangfire Scheduler

                var schedulerDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ISchedulerService));

                if (schedulerDescriptor != null)
                {
                    services.Remove(schedulerDescriptor);
                }

                services.AddSingleton(SchedulerServiceMock.Object);

                // Build ServiceProvider

                var sp = services.BuildServiceProvider();

                using var scope = sp.CreateScope();

                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            _connection?.Dispose();
        }
    }
}