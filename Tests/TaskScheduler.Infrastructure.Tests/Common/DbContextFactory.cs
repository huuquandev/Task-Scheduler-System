using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TaskScheduler.Infrastructure.Persistence;
using MediatR;
using Moq;

namespace TaskScheduler.Infrastructure.Tests.Common
{
    public class DbContextFactory : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<ApplicationDbContext> _options;
        private readonly IPublisher _publisher = Mock.Of<IPublisher>();

        public DbContextFactory()
        {
            _connection = new SqliteConnection($"DataSource={Guid.NewGuid()};Mode=Memory;Cache=Shared");

            _connection.Open();

            _options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(_connection)
                .Options;

            using var Context = new ApplicationDbContext(_options, _publisher);

            Context.Database.EnsureCreated();
        }
        public ApplicationDbContext CreateDbContext()
        {
            return new ApplicationDbContext(_options, _publisher);
        }
        public void Dispose()
        {
            _connection.Dispose();
        }
    }
}