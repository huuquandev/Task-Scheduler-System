using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TaskScheduler.Infrastructure.Persistence;

namespace TaskScheduler.Infrastructure.Tests.Common
{
    public class DbContextFactory : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<AppDbContext> _options;

        public DbContextFactory()
        {
            _connection = new SqliteConnection($"DataSource={Guid.NewGuid()};Mode=Memory;Cache=Shared");

            _connection.Open();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(_connection)
                .Options;

            Context = new ApplicationDbContext(options);

            Context.Database.EnsureCreated();
        }
        public ApplicationDbContext CreateDbContext()
        {
            return new ApplicationDbContext(_options);
        }
        public void Dispose()
        {
            Context.Dispose();
            _connection.Dispose();
        }
    }
}