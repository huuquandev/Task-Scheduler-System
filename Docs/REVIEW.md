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

Các file sau là stub class hoàn toàn rỗng — interface chưa đăng ký implementation:

| File production (stub) | Interface cần implement |
|---|---|
| `src/.../EventHandlers/Notifications/SendEmailHandler.cs` | `INotificationHandler<TaskFailedEvent>`, `IEmailService` |
| `src/.../EventHandlers/Notifications/SendSmsHandler.cs` | `INotificationHandler<TaskFailedEvent>`, `ISmsService` |
| `src/.../EventHandlers/Metrics/TaskCompletedMetricsHandler.cs` | `INotificationHandler<TaskCompletedEvent>`, `IMetricsService` |
| `src/.../EventHandlers/Metrics/TaskFailedMetricsHandler.cs` | `INotificationHandler<TaskFailedEvent>`, `IMetricsService` |
| `src/.../EventHandlers/Logging/TaskFailedLogHandler.cs` | `INotificationHandler<TaskFailedEvent>` |

Ngoài ra `TaskCompletedLogHandler` dùng `Console.WriteLine` — không phải production-ready.

```
Hướng làm nếu muốn hoàn thiện:
1. Implement IEmailService trong Infrastructure (dùng SMTP/SendGrid).
2. Implement IMetricsService (dùng Prometheus .NET hoặc Application Insights).
3. Implement các handler để inject service và gọi đúng method.
4. Đăng ký DI trong Infrastructure/DependencyInjection.cs.
5. TaskCompletedLogHandler: thay Console.WriteLine bằng inject ILogger.
```

---

### 2.5 — [LOW] NICE-to-have chưa làm

**NICE-02 — API Versioning chính thức**

```
Hiện trạng: Route có /v1/ nhưng chỉ là string prefix, không dùng Asp.Versioning package.
Cách làm:
1. dotnet add package Asp.Versioning.Mvc
2. builder.Services.AddApiVersioning(options => { options.DefaultApiVersion = new ApiVersion(1, 0); })
3. Decorate controller với [ApiVersion("1.0")] và [Route("api/v{version:apiVersion}/tasks")]
4. Lợi ích: /v2/ có thể song song mà không ảnh hưởng /v1/
```

**NICE-03 — Email/Webhook notification khi task fail**

```
Phụ thuộc: cần làm mục 2.4 (implement SendEmailHandler) trước.
Hiện trạng: TaskFailedEvent đã được raise trong ScheduledTask.MarkAsFailed().
Khi implement handler, MediatR tự dispatch event → handler tự động nhận.
```

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
