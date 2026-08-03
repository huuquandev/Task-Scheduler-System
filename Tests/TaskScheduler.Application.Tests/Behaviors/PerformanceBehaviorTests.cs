using System;

namespace TaskScheduler.Application.Tests.Behaviors
{
    // PerformanceBehavior không tồn tại dưới dạng class riêng.
    // Logic slow-request warning (> 1000ms) đã được tích hợp trực tiếp
    // vào LoggingBehavior.Handle(). Các test tương ứng nằm trong:
    //   LoggingBehaviorTests.Handle_Should_Log_Warning_When_Handler_Exceeds_Threshold
    //   LoggingBehaviorTests.Handle_Should_Not_Log_Warning_When_Handler_Is_Fast
    public class PerformanceBehaviorTests { }
}
