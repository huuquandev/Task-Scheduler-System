using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
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
                services.RemoveAll<ApplicationDbContext>();
                services.RemoveAll<DbContextOptions<ApplicationDbContext>>();

                _connection = new SqliteConnection($"DataSource=testdb_{Guid.NewGuid():N};Mode=Memory;Cache=Shared");
                _connection.Open();

                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseSqlite(_connection);
                });

                services.RemoveAll<ISchedulerService>();

                services.AddSingleton(SchedulerServiceMock.Object);
            });
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            var host = base.CreateHost(builder);

            using var scope = host.Services.CreateScope();
            
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            db.Database.EnsureCreated();

            return host;
        }
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            _connection?.Dispose();
        }
    }
}