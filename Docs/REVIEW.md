# Code Review — Trạng thái thực tế & Việc cần làm tiếp theo

> Ngày review: 2026-07-12  
> Reviewer: Claude Code (cross-verify từng mục trong TODO.md với source code thực tế)  
> Phương pháp: Đọc từng file được đề cập, đối chiếu với claim trong TODO.md

---

## Tóm tắt nhanh

| Nhóm | Tổng mục | Đã xong (trong code) | Còn thực sự cần làm |
|---|---|---|---|
| BUG (compile/logic) | 13 | 13 | 0 |
| GAP (production code) | 4 | 4 | 0 |
| TEST (coverage) | 5 | 2 | 3 |
| API integration tests | 7 | 5 | 1 (+ 1 cần verify) |
| NICE-to-have | 3 | 1 (rate limiting) | 2 |
| **Vấn đề mới phát hiện** | 2 | — | 2 |

**Kết luận tổng thể:** Phần lớn TODO.md đã được fix vào code. Còn lại là: 3 file test trống cần viết, 1 API test thiếu, 2 vấn đề mới không có trong TODO, và 2 NICE-to-have chưa làm.

---

## Phần 1 — Các mục trong TODO.md đã được fix (không cần làm lại)

Tất cả BUG-01 đến BUG-13 và GAP-01 đến GAP-04 đã được sửa vào code. Chi tiết:

- **BUG-01** — `DbContextFactory` đúng type và `_options` được gán đúng.
- **BUG-02** — `TaskRepositoryTests` dùng `seedContext.SaveChangesAsync()` đúng.
- **BUG-03** — Assertion dùng `result.Command` đúng.
- **BUG-04** — `MarkAsFailed("Test failure reason")` có argument.
- **BUG-05** — Dùng `deletedTask.SoftDelete()` thay vì object initializer.
- **BUG-06** — `UnscheduleTaskAsync(existingTask.Id)` truyền Guid đúng.
- **BUG-07** — Khởi tạo `new PauseTaskHandler(...)` đúng.
- **BUG-08** — Dùng `.MarkAsActive()`, `.SoftDelete()` v.v thay vì object initializer.
- **BUG-09** — `UnscheduleTaskAsync(existingTask.Id)` đúng.
- **BUG-10** — Dùng parameterized constructor cho `ScheduledTask` trong test.
- **BUG-11** — Tạo `User` object trước khi truyền vào `GenerateJwtToken`.
- **BUG-12** — 4 test method có tên riêng biệt.
- **BUG-13** — Constructor `TaskExecutionLog(taskId)` đúng 1 tham số.
- **GAP-01** — `RescheduleTaskAsync` đã delegate sang `ScheduleTaskAsync`.
- **GAP-02** — `TriggerTaskHandler` đã guard status `Active` hoặc `Failed`.
- **GAP-03** — `TaskExecutionService` đã dùng `RuntimeInformation.IsOSPlatform` cross-platform.
- **GAP-04** — Constructor 1 tham số nên vấn đề semantic logId/taskId không còn.
- **API-01** — `CustomWebApplicationFactory` dùng shared cache SQLite + `EnsureCreated` đúng.
- **API-03** — Test gọi `/activate` đúng endpoint.
- **API-04** — Test activate trước rồi pause.
- **API-05** — Test activate → pause → resume.
- **API-07** — Test activate trước rồi trigger.

---

## Phần 2 — Việc CÒN LẠI thực sự cần làm

### 2.1 — [HIGH] Viết test cho 3 file trống trong Application.Tests

Ba file này tồn tại nhưng thân class hoàn toàn rỗng (không có test method nào):

**File 1:** `Tests/TaskScheduler.Application.Tests/Behaviors/LoggingBehaviorTests.cs`

Cần viết test cho `LoggingBehavior` (pipeline behavior của MediatR):

```
Cách làm:
1. Mock ILogger<LoggingBehavior<,>> hoặc dùng một ILogger in-memory.
2. Tạo một IRequest giả (dummy command/query) và một RequestHandlerDelegate giả.
3. Gọi behavior.Handle(request, next, ct).
4. Verify: log được ghi ít nhất một lần cho "Handling" và một lần cho "Handled".

Skeleton test:
- LoggingBehavior_Should_Log_Before_And_After_Handler()
- LoggingBehavior_Should_Return_Handler_Response()
```

**File 2:** `Tests/TaskScheduler.Application.Tests/Behaviors/PerformanceBehaviorTests.cs`

Cần viết test cho `PerformanceBehavior` (ghi warning khi handler chạy chậm):

```
Cách làm:
1. Tạo handler delegate giả có Task.Delay() > ngưỡng cấu hình (thường 500ms).
2. Mock ILogger.
3. Gọi behavior.Handle(...).
4. Verify: logger.Log được gọi với level Warning và message chứa elapsed time.

Skeleton test:
- PerformanceBehavior_Should_Log_Warning_When_Handler_Exceeds_Threshold()
- PerformanceBehavior_Should_Not_Log_Warning_When_Handler_Is_Fast()
```

**File 3:** `Tests/TaskScheduler.Infrastructure.Tests/Scheduling/TaskJobTests.cs`

Cần viết test cho `TaskJob.Execute(Guid taskId)`:

```
Cách làm:
1. Mock ITaskExecutionService.
2. Gọi taskJob.Execute(taskId).
3. Verify: ExecuteAsync(taskId) được gọi đúng 1 lần với đúng taskId.

Skeleton test:
- Execute_Should_Call_ExecuteAsync_With_TaskId()
```

**Hai file trống khác** (nên xóa hoặc bỏ qua vì test subject là handler rỗng trong production):

- `Tests/TaskScheduler.Infrastructure.Tests/EventHandlers/TaskCompletedEventHandlerTests.cs`
- `Tests/TaskScheduler.Infrastructure.Tests/EventHandlers/TaskCreatedEventHandlerTests.cs`

Các handler tương ứng trong production code (`SendEmailHandler`, `TaskCompletedMetricsHandler` v.v.) cũng là class rỗng → test sẽ không có gì meaningful để verify. Hoặc implement handler trước rồi viết test sau (xem mục 2.4).

---

### 2.2 — [HIGH] Viết API test còn thiếu: `GetExecutionLogs`

**File:** `Tests/TaskScheduler.Api.Tests/Controllers/TasksControllerTests.cs`

Test method `GetExecutionLogs_Should_Return_Logs` không tồn tại trong file (TODO API-06 mô tả một assertion bug, nhưng cả test method cũng không có trong code).

```
Cách làm:
1. Tạo task (POST /api/v1/tasks).
2. Activate task (POST /api/v1/tasks/{id}/activate).
3. Trigger task (POST /api/v1/tasks/{id}/trigger) — để có execution log.
4. Gọi GET /api/v1/tasks/{id}/logs (hoặc GET /api/v1/execution-logs?taskId={id}).
5. Assert: response 200, list không rỗng, log.TaskId == taskId.

Lưu ý: xác nhận đúng route của ExecutionLogsController trước khi viết.
```

---

### 2.3 — [MEDIUM] Kiểm tra và xác nhận API-02

**File:** `Tests/TaskScheduler.Api.Tests/Controllers/TasksControllerTests.cs:48-55`

Test `GetTasks_WithoutToken_Should_Return_Unauthorized` assert `HttpStatusCode.Unauthorized (401)`.

```
Rủi ro còn lại:
ExceptionHandlingMiddleware hiện map ArgumentException và InvalidOperationException → 400.
Nếu pipeline throw exception trước khi auth challenge được gửi, test sẽ nhận 400 thay vì 401.

Cách verify:
1. Chạy: dotnet test --filter "GetTasks_WithoutToken_Should_Return_Unauthorized"
2. Nếu pass → không cần làm gì.
3. Nếu fail với actual 400: kiểm tra thứ tự middleware trong Program.cs.
   - UseAuthentication() phải đứng trước ExceptionHandlingMiddleware, hoặc
   - ExceptionHandlingMiddleware cần xử lý riêng trường hợp 401.
```

---

### 2.4 — [LOW] Implement các Event Handler còn trống

#### Hiện trạng thực tế

Tất cả handler dưới đây là **stub class rỗng** — chưa implement interface, chưa đăng ký DI:

| File | Interface cần implement | Service phụ thuộc |
|---|---|---|
| `src/TaskScheduler.Application/EventHandlers/Notifications/SendEmailHandler.cs` | `INotificationHandler<DomainEventNotification<TaskFailedEvent>>` | `IEmailService` |
| `src/TaskScheduler.Application/EventHandlers/Notifications/SendSmsHandler.cs` | `INotificationHandler<DomainEventNotification<TaskFailedEvent>>` | `ISmsService` |
| `src/TaskScheduler.Application/EventHandlers/Metrics/TaskCompletedMetricsHandler.cs` | `INotificationHandler<DomainEventNotification<TaskCompletedEvent>>` | `IMetricsService` |
| `src/TaskScheduler.Application/EventHandlers/Metrics/TaskFailedMetricsHandler.cs` | `INotificationHandler<DomainEventNotification<TaskFailedEvent>>` | `IMetricsService` |
| `src/TaskScheduler.Application/EventHandlers/Logging/TaskFailedLogHandler.cs` | `INotificationHandler<DomainEventNotification<TaskFailedEvent>>` | `ILogger` |

`TaskCompletedLogHandler` đã implement interface nhưng dùng `Console.WriteLine` thay vì `ILogger`.

Tham chiếu pattern đúng: xem `TaskCompletedLogHandler` — đây là handler duy nhất hoàn chỉnh, dùng `DomainEventNotification<TEvent>` wrapper, không phải `TEvent` trực tiếp.

---

#### Bước 1 — Fix TaskCompletedLogHandler (nhanh nhất, không phụ thuộc gì)

**File:** `src/TaskScheduler.Application/EventHandlers/Logging/TaskCompletedLogHandler.cs`

Thay `Console.WriteLine` bằng `ILogger`:

```csharp
public class TaskCompletedLogHandler : INotificationHandler<DomainEventNotification<TaskCompletedEvent>>
{
    private readonly ILogger<TaskCompletedLogHandler> _logger;

    public TaskCompletedLogHandler(ILogger<TaskCompletedLogHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(DomainEventNotification<TaskCompletedEvent> notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Task completed: {TaskId}", notification.DomainEvent.TaskId);
        return Task.CompletedTask;
    }
}
```

---

#### Bước 2 — Implement TaskFailedLogHandler (không phụ thuộc service ngoài)

**File:** `src/TaskScheduler.Application/EventHandlers/Logging/TaskFailedLogHandler.cs`

```csharp
using MediatR;
using Microsoft.Extensions.Logging;
using TaskScheduler.Application.Common.EventNotifications;
using TaskScheduler.Domain.Events;

namespace TaskScheduler.Application.EventHandlers.Logging
{
    public class TaskFailedLogHandler : INotificationHandler<DomainEventNotification<TaskFailedEvent>>
    {
        private readonly ILogger<TaskFailedLogHandler> _logger;

        public TaskFailedLogHandler(ILogger<TaskFailedLogHandler> logger)
        {
            _logger = logger;
        }

        public Task Handle(DomainEventNotification<TaskFailedEvent> notification, CancellationToken cancellationToken)
        {
            _logger.LogError("Task failed: {TaskId}. Reason: {Reason}",
                notification.DomainEvent.TaskId,
                notification.DomainEvent.Reason);
            return Task.CompletedTask;
        }
    }
}
```

Không cần đăng ký DI riêng — MediatR tự scan assembly qua `RegisterServicesFromAssembly` trong `Application/DependencyInjection.cs`.

---

#### Bước 3 — Implement Metrics Handlers (phụ thuộc IMetricsService)

`IMetricsService` đã có interface tại `src/TaskScheduler.Application/Interfaces/IMetricsService.cs` với 2 method:
- `IncrementCompletedTasksAsync()`
- `IncrementFailedTasksAsync()`

**3a. Implement handlers:**

`src/TaskScheduler.Application/EventHandlers/Metrics/TaskCompletedMetricsHandler.cs`:

```csharp
using MediatR;
using TaskScheduler.Application.Common.EventNotifications;
using TaskScheduler.Application.Interfaces;
using TaskScheduler.Domain.Events;

namespace TaskScheduler.Application.EventHandlers.Metrics
{
    public class TaskCompletedMetricsHandler : INotificationHandler<DomainEventNotification<TaskCompletedEvent>>
    {
        private readonly IMetricsService _metrics;

        public TaskCompletedMetricsHandler(IMetricsService metrics)
        {
            _metrics = metrics;
        }

        public Task Handle(DomainEventNotification<TaskCompletedEvent> notification, CancellationToken cancellationToken)
            => _metrics.IncrementCompletedTasksAsync();
    }
}
```

`src/TaskScheduler.Application/EventHandlers/Metrics/TaskFailedMetricsHandler.cs`:

```csharp
using MediatR;
using TaskScheduler.Application.Common.EventNotifications;
using TaskScheduler.Application.Interfaces;
using TaskScheduler.Domain.Events;

namespace TaskScheduler.Application.EventHandlers.Metrics
{
    public class TaskFailedMetricsHandler : INotificationHandler<DomainEventNotification<TaskFailedEvent>>
    {
        private readonly IMetricsService _metrics;

        public TaskFailedMetricsHandler(IMetricsService metrics)
        {
            _metrics = metrics;
        }

        public Task Handle(DomainEventNotification<TaskFailedEvent> notification, CancellationToken cancellationToken)
            => _metrics.IncrementFailedTasksAsync();
    }
}
```

**3b. Tạo concrete implementation trong Infrastructure:**

Tạo file `src/TaskScheduler.Infrastructure/Services/MetricsService.cs`. Có 2 lựa chọn:

- **Đơn giản (in-memory counter):** Dùng tạm trong development, không persist qua restart.
- **Prometheus .NET:** Cài `prometheus-net.AspNetCore`, dùng `Counter.Inc()` — recommended cho production.

Skeleton đơn giản trước:

```csharp
using TaskScheduler.Application.Interfaces;

namespace TaskScheduler.Infrastructure.Services
{
    public class MetricsService : IMetricsService
    {
        public Task IncrementCompletedTasksAsync()
        {
            // TODO: thay bằng Prometheus Counter hoặc Application Insights metric
            return Task.CompletedTask;
        }

        public Task IncrementFailedTasksAsync()
        {
            // TODO: thay bằng Prometheus Counter hoặc Application Insights metric
            return Task.CompletedTask;
        }
    }
}
```

**3c. Đăng ký DI** trong `src/TaskScheduler.Infrastructure/DependencyInjection.cs`, thêm vào cuối block `services.AddScoped`:

```csharp
services.AddScoped<IMetricsService, MetricsService>();
```

---

#### Bước 4 — Implement Email/Webhook Notification (xem tiếp mục 2.5)

`SendEmailHandler` và `SendSmsHandler` phụ thuộc `IEmailService` và `ISmsService` — xem chi tiết ở mục 2.5 bên dưới.

---

### 2.5 — Email/Webhook Notification khi task fail

`TaskFailedEvent` đã được raise trong `ScheduledTask.MarkAsFailed()`. MediatR tự dispatch qua `DomainEventNotification<TaskFailedEvent>` wrapper. Chỉ cần implement handler + service là xong.

#### Bước 1 — Implement SendEmailHandler

**File:** `src/TaskScheduler.Application/EventHandlers/Notifications/SendEmailHandler.cs`

`IEmailService.SendEmailAsync(EmailMessage)` đã có interface tại `src/TaskScheduler.Application/Interfaces/IEmailService.cs`. `EmailMessage` model nằm ở `TaskScheduler.Application.Common.Models`.

```csharp
using MediatR;
using TaskScheduler.Application.Common.EventNotifications;
using TaskScheduler.Application.Common.Models;
using TaskScheduler.Application.Interfaces;
using TaskScheduler.Domain.Events;

namespace TaskScheduler.Application.EventHandlers.Notifications
{
    public class SendEmailHandler : INotificationHandler<DomainEventNotification<TaskFailedEvent>>
    {
        private readonly IEmailService _emailService;

        public SendEmailHandler(IEmailService emailService)
        {
            _emailService = emailService;
        }

        public Task Handle(DomainEventNotification<TaskFailedEvent> notification, CancellationToken cancellationToken)
        {
            var ev = notification.DomainEvent;
            var message = new EmailMessage
            {
                // TODO: lấy email người nhận từ config hoặc task settings
                To = "admin@example.com",
                Subject = $"Task {ev.TaskId} failed",
                Body = $"Task {ev.TaskId} failed with reason: {ev.Reason}"
            };
            return _emailService.SendEmailAsync(message);
        }
    }
}
```

#### Bước 2 — Tạo SmtpEmailService trong Infrastructure

Tạo `src/TaskScheduler.Infrastructure/Services/SmtpEmailService.cs`:

```csharp
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using TaskScheduler.Application.Common.Models;
using TaskScheduler.Application.Interfaces;

namespace TaskScheduler.Infrastructure.Services
{
    public class SmtpEmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public SmtpEmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(EmailMessage message)
        {
            var host = _config["Smtp:Host"];
            var port = int.Parse(_config["Smtp:Port"] ?? "587");
            var user = _config["Smtp:User"];
            var pass = _config["Smtp:Password"];

            using var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(user, pass),
                EnableSsl = true
            };

            var mail = new MailMessage(user!, message.To, message.Subject, message.Body);
            await client.SendMailAsync(mail);
        }
    }
}
```

Thêm vào `appsettings.json`:

```json
"Smtp": {
  "Host": "smtp.gmail.com",
  "Port": "587",
  "User": "your@email.com",
  "Password": "your-app-password"
}
```

Đăng ký DI trong `Infrastructure/DependencyInjection.cs`:

```csharp
services.AddScoped<IEmailService, SmtpEmailService>();
```

#### Bước 3 — Implement SendSmsHandler (tùy chọn, có thể bỏ qua)

Pattern giống `SendEmailHandler` — inject `ISmsService`, gọi `SendSmsAsync(SmsMessage)`. Cần có provider SMS thực tế (Twilio, v.v.) mới có ý nghĩa.

Nếu chưa có provider: để handler rỗng hoặc log warning thay vì implement. Không nên implement nửa vời.

---

## Phần 3 — Vấn đề mới phát hiện (không có trong TODO.md)

### BUG-NEW-01 — [MEDIUM] `TaskExecutionService.ScheduleRetry` dùng static Hangfire API

**File:** `src/TaskScheduler.Infrastructure/Scheduling/TaskExecutionService.cs:206`

**Vấn đề:**

```csharp
// Dòng 206 — dùng static API
BackgroundJob.Schedule<ITaskExecutionService>(...);

// Dòng 115 — TriggerNow dùng injected client (đúng cách)
_backgroundJobClient.Enqueue<ITaskExecutionService>(...);
```

Sự không nhất quán này có hai hệ quả:
1. **Không thể unit test** `ScheduleRetry` — static call không mock được, test sẽ cần Hangfire storage thật.
2. **Inconsistent DI** — `_backgroundJobClient` đã được inject qua constructor nhưng không dùng ở một nhánh.

**Cách fix:**

```csharp
// Thay dòng dùng static API bằng:
_backgroundJobClient.Schedule<ITaskExecutionService>(
    s => s.ExecuteAsync(task.Id, CancellationToken.None),
    delay
);
```

---

### NOTE-NEW-01 — [LOW] Duplicate using directives trong 3 Controller

**Files:**
- `src/TaskScheduler.Api/Controllers/AuthController.cs` — dòng 6 và 8 đều `using MediatR;`
- `src/TaskScheduler.Api/Controllers/ExecutionLogsController.cs` — duplicate `using System.Threading.Tasks;`
- `src/TaskScheduler.Api/Controllers/DashboardController.cs` — duplicate `using System.Threading.Tasks;`

Không gây lỗi runtime nhưng là code smell. Có thể fix nhanh bằng IDE "Remove Unnecessary Usings".

---

## Phần 4 — Thứ tự thực hiện (đề xuất)

```
Ưu tiên cao — làm trước:
  1. Chạy: dotnet test Task-scheduler-system.sln
     → Xác nhận số test pass hiện tại (baseline)

  2. [BUG-NEW-01] Fix TaskExecutionService.ScheduleRetry dùng _backgroundJobClient
     → File: src/TaskScheduler.Infrastructure/Scheduling/TaskExecutionService.cs:206

  3. [2.3] Chạy test API-02 riêng, confirm 401 hay còn fail

Ưu tiên trung bình — làm sau:
  4. [2.1] Viết LoggingBehaviorTests (3-4 test method)
  5. [2.1] Viết PerformanceBehaviorTests (2-3 test method)
  6. [2.1] Viết TaskJobTests (1-2 test method)
  7. [2.2] Viết GetExecutionLogs API integration test

Ưu tiên thấp — làm cuối:
  8. [2.4] Implement EventHandlers (Email, Metrics, Logging)
  9. [NOTE-NEW-01] Remove duplicate using directives
  10. [NICE-02] API Versioning với Asp.Versioning package
  11. [NICE-03] Email notification (sau khi có IEmailService implementation)
```

---

## Lệnh kiểm tra

```powershell
# Chạy toàn bộ test và xem kết quả
dotnet test Task-scheduler-system.sln --logger "console;verbosity=minimal"

# Chạy riêng từng project
dotnet test Tests/TaskScheduler.Domain.Tests/TaskScheduler.Domain.Tests.csproj
dotnet test Tests/TaskScheduler.Application.Tests/TaskScheduler.Application.Tests.csproj
dotnet test Tests/TaskScheduler.Infrastructure.Tests/TaskScheduler.Infrastructure.Tests.csproj
dotnet test Tests/TaskScheduler.Api.Tests/TaskScheduler.Api.Tests.csproj

# Chạy một test cụ thể
dotnet test --filter "GetTasks_WithoutToken_Should_Return_Unauthorized"
```
