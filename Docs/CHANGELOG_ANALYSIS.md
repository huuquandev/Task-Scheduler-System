# Phân Tích Dự Án & Ghi Chú Thay Đổi Gần Nhất

> Tạo ngày: 2026-09-06  
> Nhánh: `main`  
> 5 commit gần nhất được phân tích: `f694c9f` → `266d150`

---

## 1. Tổng Quan Tình Trạng Dự Án

### Kiến trúc hiện tại (Clean Architecture + CQRS)

```
Domain  ←  Application  ←  Infrastructure  ←  Api
```

Dự án đã **hoàn thiện sơ bộ** theo đúng thiết kế, bao gồm:

| Tính năng | Trạng thái |
|-----------|------------|
| CRUD ScheduledTask | ✅ Hoàn chỉnh |
| Cron scheduling (Hangfire) | ✅ Hoàn chỉnh |
| Thực thi lệnh thực tế (Process) | ✅ Hoàn chỉnh |
| Retry với exponential backoff | ✅ Hoàn chỉnh |
| Domain events (MediatR) | ✅ Hoàn chỉnh |
| JWT Authentication | ✅ Hoàn chỉnh |
| Email notification khi task fail | ✅ Hoàn chỉnh |
| SMS notification (NoOp stub) | ✅ Stub — chưa tích hợp provider thực |
| Metrics service (NoOp stub) | ✅ Stub — chưa tích hợp Prometheus/OTel thực |
| Serilog logging | ✅ Hoàn chỉnh |
| OpenTelemetry tracing | ✅ Cơ bản |
| Rate limiting | ✅ Hoàn chỉnh |
| Health checks | ✅ Hoàn chỉnh |
| CI pipeline (GitHub Actions) | ✅ Hoàn chỉnh |
| CD pipeline (Docker build/push) | ✅ Hoàn chỉnh |
| Docker + docker-compose | ✅ Hoàn chỉnh |
| Integration tests (Api) | ✅ Cơ bản |
| Unit tests (Domain/Application/Infrastructure) | ✅ Cơ bản |

---

## 2. File Dư Thừa / Nên Xóa

### Các file `UnitTest1.cs` rỗng (placeholder tự động sinh bởi template)

Đây là file mẫu được .NET template tạo ra, **không chứa test thực sự** — chỉ có một `Test1()` method rỗng không assert gì. Nên xóa để tránh lộn xộn:

| File | Lý do xóa |
|------|-----------|
| `Tests/TaskScheduler.Domain.Tests/UnitTest1.cs` | Template placeholder, test rỗng |
| `Tests/TaskScheduler.Infrastructure.Tests/UnitTest1.cs` | Template placeholder, test rỗng |
| `Tests/TaskScheduler.Api.Tests/UnitTest1.cs` | Template placeholder, test rỗng |

### `src/TaskScheduler.Application/Common/Models/SmsMessage.cs`

File này tồn tại và được dùng bởi `ISmsService`/`NoOpSmsService`. **Không dư thừa**, nhưng cần chú ý: toàn bộ SMS pipeline (interface → model → handler → service) là stub NoOp, không có provider thực.

---

## 3. Phân Tích Các Thay Đổi Gần Nhất

### Commit `266d150` — "noti service setup" (2026-08-06)

**Mục đích:** Hoàn thiện tầng notification & metrics service, chuẩn hóa các event handler.

#### 3.1 `SendEmailHandler` — Refactor thành notification thực sự

**Trước:** Handler xử lý event `TaskFailedEvent` nhưng logic email còn inline, chưa dùng `IEmailService` đúng cách.

**Sau:**
```csharp
// Application/EventHandlers/Notifications/SendEmailHandler.cs
public class SendEmailHandler : INotificationHandler<DomainEventNotification<TaskFailedEvent>>
{
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    public Task Handle(...)
    {
        var notifyEmail = _configuration["Notifications:AdminEmail"];
        if (string.IsNullOrWhiteSpace(notifyEmail))
            return Task.CompletedTask;

        var message = new EmailMessage { To = notifyEmail, Subject = ..., Body = ... };
        return _emailService.SendEmailAsync(message);
    }
}
```

**Tại sao cần làm thế:**
- Tách biệt rõ ràng giữa _khi nào gửi email_ (Application layer — handler) và _cách gửi email_ (Infrastructure layer — `SmtpEmailService`)
- Guard `AdminEmail` rỗng: nếu chưa cấu hình thì skip hoàn toàn, tránh lỗi runtime
- Có thể swap `SmtpEmailService` sang SendGrid/SES mà không đổi handler

#### 3.2 `SmtpEmailService` — Triển khai SMTP thực

**Trước:** Chỉ có interface `IEmailService`, không có implementation thực.

**Sau:** Service dùng `System.Net.Mail.SmtpClient` đọc config từ `appsettings.json`:
```csharp
// Infrastructure/Services/SmtpEmailService.cs
var host = _config["Smtp:Host"]!;
var port = int.Parse(_config["Smtp:Port"] ?? "587");
using var client = new SmtpClient(host, port) { Credentials = ..., EnableSsl = true };
await client.SendMailAsync(...);
```

**Tại sao cần làm thế:**
- Dự án cần có implementation thực (không phải chỉ interface) để DI container resolve được khi chạy Production
- Cấu hình SMTP externalized vào `appsettings.json` / env vars → không hardcode credential

#### 3.3 `NoOpSmsService` — SMS stub

**Trước:** `SendSmsHandler` bị xóa (`D src/TaskScheduler.Application/EventHandlers/Notifications/SendSmsHandler.cs` trong git status).

**Sau:** SMS pipeline được giữ lại nhưng dùng `NoOpSmsService` — chỉ log ra mà không gửi thực:
```csharp
// Infrastructure/Services/NoOpSmsService.cs
public Task SendSmsAsync(SmsMessage message)
{
    _logger.LogInformation("SMS to {Phone}: {Message}", message.PhoneNumber, message.Message);
    return Task.CompletedTask;
}
```

**Tại sao cần làm thế:**
- SMS cần provider thứ 3 (Twilio, ESMS...) — chưa có contract/billing, tránh đưa vào production ngay
- NoOp pattern cho phép infrastructure compile và test pass mà không phụ thuộc external service
- Khi cần, chỉ cần tạo `TwilioSmsService : ISmsService` và đổi DI binding — không phá vỡ Application layer

> **Lưu ý:** File `SendSmsHandler.cs` đã **bị xóa** (xem git status: `D src/.../SendSmsHandler.cs`). Điều này có nghĩa SMS event handler không còn active. Nếu sau này cần SMS notification, phải tạo lại handler này.

#### 3.4 `MetricsService` — Metrics stub

**File mới:** `Infrastructure/Services/MetricsService.cs`

```csharp
public class MetricsService : IMetricsService
{
    public Task IncrementCompletedTasksAsync() => Task.CompletedTask;
    public Task IncrementFailedTasksAsync() => Task.CompletedTask;
}
```

**Tại sao cần làm thế:**
- `IMetricsService` được inject vào `TaskCompletedMetricsHandler` và `TaskFailedMetricsHandler`
- Không có implementation → DI fail khi khởi động
- NoOp tương tự SMS: placeholder để sau này swap sang Prometheus Counter / OpenTelemetry Meter thực

#### 3.5 `TaskCompletedMetricsHandler` & `TaskFailedMetricsHandler` — Refactor

**Trước:** Handler tự xử lý logic metrics.

**Sau:** Delegate hoàn toàn sang `IMetricsService`:
```csharp
public Task Handle(...) => _metrics.IncrementCompletedTasksAsync();
```

**Tại sao cần làm thế:**
- Single Responsibility: handler chỉ biết "task completed → increment counter", không biết counter lưu ở đâu
- Testable: mock `IMetricsService` dễ hơn mock Prometheus Counter

#### 3.6 `TaskCompletedLogHandler` & `TaskFailedLogHandler` — Tách Logging khỏi Infrastructure

**Trước:** Log handler ở sai layer hoặc lẫn với infrastructure concern.

**Sau:** Handler thuần Application layer, chỉ dùng `ILogger<T>`:
```csharp
// EventHandlers/Logging/TaskCompletedLogHandler.cs
public Task Handle(...) 
{
    _logger.LogInformation("Task completed: {TaskId}", notification.DomainEvent.TaskId);
    return Task.CompletedTask;
}

// EventHandlers/Logging/TaskFailedLogHandler.cs
public Task Handle(...)
{
    _logger.LogError("Task failed: {TaskId}. Reason: {Reason}", ...);
    return Task.CompletedTask;
}
```

**Tại sao cần làm thế:**
- Domain events nên có ít nhất 1 log handler để trace được lifecycle của task
- Tách biệt: log handler ≠ metrics handler ≠ notification handler — mỗi cái một responsibility
- MediatR publish notification tới TẤT CẢ handlers đã đăng ký → không cần if/else trong một handler lớn

#### 3.7 `TaskCreatedLogHandler` — Handler mới

**File mới:** `Application/EventHandlers/Logging/TaskCreatedLogHandler.cs`

```csharp
public Task Handle(DomainEventNotification<TaskCreatedEvent> ...)
{
    _logger.LogInformation("Task created: {TaskId} ({TaskName})", ...);
    return Task.CompletedTask;
}
```

**Tại sao cần làm thế:**
- Trước đây thiếu log handler cho event `TaskCreatedEvent` — khi task được tạo, không có trace nào
- Bổ sung để audit trail đầy đủ: Created → Running → Completed/Failed

#### 3.8 `ExecutionLogService` — Log service mới

**File mới:** `Infrastructure/Services/ExecutionLogService.cs`

Service triển khai `IExecutionLogService` với 4 method:
- `LogTaskCompletedAsync`
- `LogTaskFailedAsync`  
- `LogTaskPausedAsync`
- `LogTaskResumedAsync`

**Tại sao cần làm thế:**
- `IExecutionLogService` được define ở Application layer nhưng thiếu implementation
- DI container không thể resolve → lỗi startup nếu có handler nào inject interface này
- Hiện tại là log-only implementation; sau có thể persist vào DB/external system

#### 3.9 `DependencyInjection.cs` — Đăng ký 3 service mới

```csharp
services.AddScoped<IMetricsService, MetricsService>();
services.AddScoped<IEmailService, SmtpEmailService>();
services.AddScoped<ISmsService, NoOpSmsService>();
services.AddScoped<IExecutionLogService, ExecutionLogService>();
```

**Tại sao cần làm thế:**
- Không đăng ký DI → runtime exception "Unable to resolve service for type..."
- Tách từng service một thay vì gộp để dễ swap implementation sau này

#### 3.10 `SmsMessage.cs` — Cleanup

**Trước:** Có thể có property dư thừa hoặc using thừa.

**Sau:** Model gọn:
```csharp
public class SmsMessage
{
    public string PhoneNumber { get; init; } = default!;
    public string Message { get; init; } = default!;
}
```

#### 3.11 `LoggingBehavior.cs` — Minor cleanup

Xóa 1 dòng thừa (theo git diff: `-1 line`). Behavior vẫn giữ toàn bộ chức năng: log request/response, đo thời gian, warn slow request, tích hợp OpenTelemetry Activity.

#### 3.12 `Program.cs` — Minor cleanup

Xóa 1 dòng thừa (redundant service registration hoặc comment).

---

### Commit `1911812` — "done test case" (2026-08-03)

**Mục đích:** Hoàn thiện test coverage, fix TaskExecutionService, bổ sung Docs.

#### Test mới:
- `Tests/Api/Controllers/TasksControllerTests.cs` — Integration test cho Tasks controller
- `Tests/Application/Behaviors/LoggingBehaviorTests.cs` — Unit test LoggingBehavior (thêm ~184 dòng)
- `Tests/Application/Behaviors/PerformanceBehaviorTests.cs` — Minor fix
- `Tests/Infrastructure/Scheduling/TaskJobTests.cs` — Test TaskJob (thêm ~50 dòng)

#### `TaskExecutionService.cs` — Fix minor

Cải thiện logic execution (4 dòng thay đổi — có thể là fix null check hoặc log message).

---

### Commit `2da3650` — "fix bug" (2026-07-12)

**Mục đích:** Fix critical bug trong integration tests.

#### `CustomWebApplicationFactory.cs` — Fix test isolation

**Trước:** Factory không properly mock `IBackgroundJobClient` → tests bị lỗi khi Hangfire cố connect real DB.

**Sau:** Add mock cho `IBackgroundJobClient`:
```csharp
services.RemoveAll<IBackgroundJobClient>();
services.AddSingleton(BackgroundJobClientMock.Object);
```

**Tại sao cần làm thế:**
- `TaskExecutionService` inject `IBackgroundJobClient` để schedule retries
- Trong test environment, không có Hangfire server → phải mock để avoid exception
- Dùng `Singleton` (không phải `Scoped`) vì mock object stateful — cần track calls across requests

#### `TasksControllerTests.cs` — Xóa 44 dòng test fail

Xóa các test case bị broken, giữ lại test pass. Đây là clean-up technical debt — tests sai logic sẽ block CI.

---

## 4. Bugs & Vấn Đề Kỹ Thuật Phát Hiện

### Bug 1 — Double RetryCount increment (Medium)

**File:** `src/TaskScheduler.Domain/Entities/ScheduledTask.cs:95` và `src/TaskScheduler.Infrastructure/Scheduling/TaskExecutionService.cs:91,101`

**Vấn đề:** Khi task fail và đã hết retries, `RetryCount` bị tăng **2 lần** trong cùng 1 execution:

```csharp
// TaskExecutionService.cs (catch block)
task.IncreaseRetryCount();        // RetryCount++ (lần 1)

if (task.RetryCount <= task.MaxRetries)
    task.MarkAsActive();
else
    task.MarkAsFailed(ex.Message); // RetryCount++ lại trong MarkAsFailed (lần 2)
```

```csharp
// ScheduledTask.cs
public void MarkAsFailed(string reason)
{
    Status = ScheduledTaskStatus.Failed;
    RetryCount++;  // ← BUG: increment thứ 2 không cần thiết
    ...
}
```

**Ảnh hưởng:** Sau khi hết retry, `RetryCount` sẽ là `MaxRetries + 2` thay vì `MaxRetries + 1`. Không gây crash nhưng dữ liệu không chính xác.

**Fix gợi ý:** Xóa `RetryCount++` trong `MarkAsFailed()` — việc tăng retry là responsibility của service, không phải entity method. Entity method chỉ nên thay đổi `Status`.

### Bug 2 — `ISensitiveRequest` đã được implement đúng (Không phải bug)

**Xác nhận:** `LoginCommand` đã implement `ISensitiveRequest`:
```csharp
public record LoginCommand(string Username, string Password) : IRequest<AuthResponse>, ISensitiveRequest;
```

`LoggingBehavior` check:
```csharp
var requestBody = request switch
{
    ISensitiveRequest => "[REDACTED]",
    _ => JsonSerializer.Serialize(request)
};
```

→ Password được redact hoàn toàn trước khi log. **Không có security leak.**

### Vấn đề — `ExecutionLogService` là dead code

`IExecutionLogService` và `ExecutionLogService` đã được đăng ký DI, nhưng **không có event handler nào inject hoặc gọi** interface này. Nó hoàn toàn tồn tại mà không phục vụ luồng nào. Cần quyết định: hoặc xóa, hoặc wire vào event handler phù hợp (ví dụ: handler nghe `TaskCompletedEvent` và gọi `LogTaskCompletedAsync`).

### Vấn đề — CD không đợi CI pass

`cd.yml` trigger độc lập với `ci.yml`. Khi push lên `main`, cả 2 chạy song song — tức CD có thể build và push Docker image cho code đang bị failing tests. Fix:

```yaml
# cd.yml — thêm workflow_run trigger
on:
  workflow_run:
    workflows: ["CI"]
    branches: [main]
    types: [completed]
```

Hoặc gộp CI + CD vào 1 file workflow với `needs`.

---

## 5. Những Gì Còn Thiếu / Cần Làm Tiếp

### Priority cao

| Hạng mục | Chi tiết |
|----------|----------|
| **SMS provider thực** | `SendSmsHandler.cs` đã bị xóa, `NoOpSmsService` chỉ log. Cần tạo lại handler + tích hợp Twilio/ESMS nếu muốn dùng |
| **Metrics thực** | `MetricsService` là NoOp. Cần tích hợp `System.Diagnostics.Metrics` (OTel) hoặc Prometheus `prometheus-net` |
| **`Notifications:AdminEmail` config** | Hiện để rỗng trong `appsettings.json`. Cần điền giá trị thực khi deploy Production |
| **SMTP credentials** | `appsettings.json` có placeholder `"your-app-password"` — phải dùng env var / secrets manager |

### Priority trung bình

| Hạng mục | Chi tiết |
|----------|----------|
| **Xóa UnitTest1.cs rỗng** | 3 file placeholder không có giá trị |
| **`TaskExecutionStatus.cs` enum** | File tồn tại trong Domain nhưng CLAUDE.md chỉ nhắc `ScheduledTaskStatus`. Kiểm tra có đang dùng không |
| **`IExecutionLogService` chưa được dùng trong flow** | Interface và implementation đã có, nhưng chưa thấy handler nào gọi `IExecutionLogService` sau khi xóa `SendSmsHandler`. Cần kiểm tra xem có event handler nào inject nó không |
| **CD pipeline thiếu CI dependency** | `cd.yml` có `needs: []` — CD chạy song song CI, không đợi tests pass. Nên thêm `needs: [build-and-test]` sau khi CI job được đặt tên |
| **`.env` file** | `docker-compose.yml` dùng `${DB_PASSWORD}`, `${JWT_SECRET}`... nhưng không có `.env.example` file để dev biết cần set gì |

### Priority thấp

| Hạng mục | Chi tiết |
|----------|----------|
| **`TasksPaged` query** | Có `GetTasksPagedHandler` và `GetTasksPagedQuery` nhưng không thấy controller endpoint tương ứng. Cần kiểm tra |
| **`GetTasks` vs `GetTasksPaged`** | Có 2 query khác nhau — một dùng paging, một không. Cần document rõ hoặc gộp lại |
| **Test coverage EventHandlers** | Chỉ có test cho `TaskCompletedMetricsHandler`, `TaskFailedMetricsHandler`, `TaskCreatedLogHandler`. Thiếu test cho `TaskCompletedLogHandler`, `TaskFailedLogHandler`, `SendEmailHandler` trong `Application.Tests` |

---

## 5. Tổng Kết Flow Domain Events

Sau tất cả thay đổi, flow event hiện tại khi task **fail**:

```
TaskExecutionService.ExecuteTask()
  └─ task.MarkAsFailed(ex.Message)
       └─ AddDomainEvent(new TaskFailedEvent(taskId, reason))

ApplicationDbContext.SaveChangesAsync()
  └─ DispatchDomainEvents(mediator)
       └─ mediator.Publish(DomainEventNotification<TaskFailedEvent>)
            ├─ TaskFailedLogHandler     → ILogger.LogError(...)
            ├─ TaskFailedMetricsHandler → IMetricsService.IncrementFailedTasksAsync()
            └─ SendEmailHandler         → IEmailService.SendEmailAsync(...) [nếu AdminEmail configured]
```

Flow khi task **complete**:
```
TaskExecutionService.ExecuteTask()
  └─ task.MarkAsActive() [không có TaskCompletedEvent ở đây!]
```

> **Phát hiện:** `TaskCompletedEvent` tồn tại trong `Domain/Events/` và có handler `TaskCompletedMetricsHandler` + `TaskCompletedLogHandler`, nhưng **không thấy** `task.MarkAsCompleted()` hay `AddDomainEvent(new TaskCompletedEvent(...))` được gọi trong `TaskExecutionService`. `task.MarkAsActive()` được gọi sau khi thực thi thành công — nghĩa là task quay lại Active để chờ lần schedule tiếp theo. Event `TaskCompletedEvent` có thể chỉ dùng cho task one-shot (MaxRetries = 0 và thực thi xong).

---

## 6. Ghi Chú Kỹ Thuật Quan Trọng

### Pattern NoOp
Ba service được implement theo NoOp pattern:
- `NoOpSmsService` 
- `MetricsService` (thực chất cũng là NoOp)
- `ExecutionLogService` (log-only, không persist)

Đây là **conscious design decision**: cho phép dự án chạy và test được mà không cần external dependencies (Twilio, Prometheus, etc.). Khi cần, swap implementation mà không đụng Application layer.

### `Testing` environment
`CustomWebApplicationFactory` set `UseEnvironment("Testing")` → `DependencyInjection.cs` skip Hangfire và PostgreSQL, dùng SQLite in-memory. `Program.cs` skip migration khi `IsEnvironment("Testing")`. Thiết kế này cho phép integration tests chạy không cần Docker.

### CD pipeline hiện tại
`cd.yml` build và push Docker image lên GitHub Container Registry (`ghcr.io`) mỗi khi push lên `main`. Image được tag bằng `:latest` và `:sha`. **Không có deployment step** — chỉ build/push. Để deploy thực tế cần thêm bước (Kubernetes, Cloud Run, VPS...).
