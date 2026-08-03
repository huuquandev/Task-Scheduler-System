using System;
using System.Threading.Tasks;
using Moq;
using TaskScheduler.Application.Interfaces;
using TaskScheduler.Infrastructure.Scheduling;
using Xunit;

namespace TaskScheduler.Infrastructure.Tests.Scheduling
{
    public class TaskJobTests
    {
        private readonly Mock<ITaskExecutionService> _executionServiceMock;
        private readonly TaskJob _taskJob;

        public TaskJobTests()
        {
            _executionServiceMock = new Mock<ITaskExecutionService>();
            _taskJob = new TaskJob(_executionServiceMock.Object);
        }

        [Fact]
        public async Task Execute_Should_Call_ExecuteTask_With_Correct_TaskId()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            _executionServiceMock
                .Setup(x => x.ExecuteTask(taskId))
                .Returns(Task.CompletedTask);

            // Act
            await _taskJob.Execute(taskId);

            // Assert
            _executionServiceMock.Verify(x => x.ExecuteTask(taskId), Times.Once);
        }

        [Fact]
        public async Task Execute_Should_Not_Call_ExecuteTask_With_Different_TaskId()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var otherId = Guid.NewGuid();
            _executionServiceMock
                .Setup(x => x.ExecuteTask(taskId))
                .Returns(Task.CompletedTask);

            // Act
            await _taskJob.Execute(taskId);

            // Assert
            _executionServiceMock.Verify(x => x.ExecuteTask(otherId), Times.Never);
        }
    }
}
