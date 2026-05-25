using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskScheduler.Domain.Entities;

namespace TaskScheduler.Domain.Tests.Builders
{
    public class ExecutionLogBuilder
    {
         private Guid _taskId = Guid.NewGuid();
        public TaskExecutionLogBuilder WithTaskId(Guid taskId)
        {
            _taskId = taskId;
            return this;
        }

        public TaskExecutionLog Build()
        {
            return new TaskExecutionLog(_taskId);
        }
    }
}