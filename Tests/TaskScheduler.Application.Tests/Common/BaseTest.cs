using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Moq;
using TaskScheduler.Application.Interfaces;
using TaskScheduler.Application.Mappings;

namespace TaskScheduler.Application.Tests.Common
{
    public abstract class BaseTest
    {
        protected readonly IMapper Mapper;

        protected readonly Mock<ITaskRepository> MockTaskRepository;

        protected readonly Mock<ITaskExecutionLogRepository> MockLogRepository;

        protected BaseTest()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddMaps(typeof(TaskScheduler.Application
                    .DependencyInjection)
                    .Assembly);
            });

            Mapper = config.CreateMapper();

            MockTaskRepository = new Mock<ITaskRepository>();

            MockLogRepository = new Mock<ITaskExecutionLogRepository>();
        }
    }
}