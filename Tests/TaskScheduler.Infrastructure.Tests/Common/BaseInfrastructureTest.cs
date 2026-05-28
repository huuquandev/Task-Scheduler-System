using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace TaskScheduler.Infrastructure.Tests.Common
{
    public class BaseInfrastructureTest : IDisposable
    {
        protected readonly DbContextFactory Factory;

        protected BaseInfrastructureTest()
        {
            Factory = new DbContextFactory();
        }

        public void Dispose()
        {
            Factory.Dispose();
        }
    }
}