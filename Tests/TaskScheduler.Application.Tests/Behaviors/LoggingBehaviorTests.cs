using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using TaskScheduler.Application.Common.Behaviors;
using Xunit;

namespace TaskScheduler.Application.Tests.Behaviors
{
    public class LoggingBehaviorTests
    {
        private readonly Mock<ILogger<LoggingBehavior<TestRequest, TestResponse>>> _loggerMock;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly LoggingBehavior<TestRequest, TestResponse> _behavior;

        public LoggingBehaviorTests()
        {
            _loggerMock = new Mock<ILogger<LoggingBehavior<TestRequest, TestResponse>>>();
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

            _behavior = new LoggingBehavior<TestRequest, TestResponse>(
                _loggerMock.Object,
                _httpContextAccessorMock.Object);
        }

        [Fact]
        public async Task Handle_Should_Log_Before_And_After_Handler()
        {
            // Arrange
            var request = new TestRequest();
            var expectedResponse = new TestResponse();
            RequestHandlerDelegate<TestResponse> next = (_) => Task.FromResult(expectedResponse);

            // Act
            await _behavior.Handle(request, next, CancellationToken.None);

            // Assert — LogInformation được gọi ít nhất 2 lần (Handling + Handled)
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Handling")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Handled")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task Handle_Should_Return_Handler_Response()
        {
            // Arrange
            var request = new TestRequest();
            var expectedResponse = new TestResponse { Value = "result" };
            RequestHandlerDelegate<TestResponse> next = (_) => Task.FromResult(expectedResponse);

            // Act
            var result = await _behavior.Handle(request, next, CancellationToken.None);

            // Assert
            result.Should().Be(expectedResponse);
        }

        [Fact]
        public async Task Handle_Should_Log_Warning_When_Handler_Exceeds_Threshold()
        {
            // Arrange — delegate chạy > 1000ms (ngưỡng cấu hình trong LoggingBehavior)
            var request = new TestRequest();
            var expectedResponse = new TestResponse();
            RequestHandlerDelegate<TestResponse> next = async (_) =>
            {
                await Task.Delay(1100);
                return expectedResponse;
            };

            // Act
            await _behavior.Handle(request, next, CancellationToken.None);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Slow request")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_Should_Not_Log_Warning_When_Handler_Is_Fast()
        {
            // Arrange — delegate chạy nhanh < 1000ms
            var request = new TestRequest();
            var expectedResponse = new TestResponse();
            RequestHandlerDelegate<TestResponse> next = (_) => Task.FromResult(expectedResponse);

            // Act
            await _behavior.Handle(request, next, CancellationToken.None);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_Should_Log_Error_And_Rethrow_When_Handler_Throws()
        {
            // Arrange
            var request = new TestRequest();
            var exception = new InvalidOperationException("handler error");
            RequestHandlerDelegate<TestResponse> next = (_) => throw exception;

            // Act
            var act = async () => await _behavior.Handle(request, next, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("handler error");

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_Should_Include_CorrelationId_From_HttpContext()
        {
            // Arrange
            var correlationId = Guid.NewGuid().ToString();
            var httpContext = new DefaultHttpContext();
            httpContext.Items["CorrelationId"] = correlationId;
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

            var behavior = new LoggingBehavior<TestRequest, TestResponse>(
                _loggerMock.Object,
                _httpContextAccessorMock.Object);

            var request = new TestRequest();
            RequestHandlerDelegate<TestResponse> next = (_) => Task.FromResult(new TestResponse());

            // Act
            await behavior.Handle(request, next, CancellationToken.None);

            // Assert — log phải chứa correlationId
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains(correlationId)),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        // --- Helper types ---

        public class TestRequest : IRequest<TestResponse> { }

        public class TestResponse
        {
            public string Value { get; set; } = string.Empty;
        }
    }
}
