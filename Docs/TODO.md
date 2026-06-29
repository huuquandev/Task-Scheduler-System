# TODO — Công việc cần làm để hoàn thành dự án

> Ngày phân tích: 2026-06-29  
> Trạng thái tổng thể: **~80% hoàn thành**. Kiến trúc đúng, luồng chính chạy được. Còn lại toàn bộ là **bug trong test** và một số gap nhỏ trong logic.

---

## Mức độ ưu tiên

- **[CRITICAL]** — Compile error, test không chạy được
- **[HIGH]** — Logic sai, test pass giả, behavior sai
- **[MEDIUM]** — Code smell, thiếu coverage
- **[LOW]** — Nice-to-have, tính năng nâng cao

---

## NHÓM 1 — Fix bug để build + test chạy được

### BUG-01 [CRITICAL] — `DbContextFactory` type sai và `_options` null

**File:** `Tests/TaskScheduler.Infrastructure.Tests/Common/DbContextFactory.cs`

**Vấn đề:**
- Dòng 14: khai báo `DbContextOptions<AppDbContext>` — type `AppDbContext` không tồn tại, phải là `ApplicationDbContext`
- Dòng 14: field `_options` được khai báo nhưng **không bao giờ được gán giá trị** → `CreateDbContext()` ở dòng 32 luôn dùng `_options = null` → mọi test dùng `Factory.CreateDbContext()` sẽ throw `NullReferenceException`

**Cách fix:**
```csharp
// TRƯỚC (sai)
private readonly DbContextOptions<AppDbContext> _options;

public DbContextFactory()
{
    _connection = new SqliteConnection(...);
    _connection.Open();

    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseSqlite(_connection)
        .Options;

    Context = new ApplicationDbContext(options);   // options gán vào Context
    Context.Database.EnsureCreated();
    // BUG: _options không bao giờ được gán!
}

// SAU (đúng)
private readonly DbContextOptions<ApplicationDbContext> _options;   // ← sửa type

public DbContextFactory()
{
    _connection = new SqliteConnection($"DataSource={Guid.NewGuid()};Mode=Memory;Cache=Shared");
    _connection.Open();

    _options = new DbContextOptionsBuilder<ApplicationDbContext>()  // ← gán vào _options
        .UseSqlite(_connection)
        .Options;

    using var context = new ApplicationDbContext(_options);
    context.Database.EnsureCreated();
}

public ApplicationDbContext CreateDbContext()
{
    return new ApplicationDbContext(_options);   // ← giờ _options có giá trị
}
```

**Ảnh hưởng:** Toàn bộ `TaskRepositoryTests`, `ExecutionLogRepositoryTests`, `UserRepositoryTests` đều fail nếu không fix cái này trước.

---

### BUG-02 [CRITICAL] — `TaskRepositoryTests` dùng biến `context` không tồn tại

**File:** `Tests/TaskScheduler.Infrastructure.Tests/Persistence/TaskRepositoryTests.cs`

**Vấn đề tại dòng 71 và 132:**
```csharp
// TRONG test GetByIdAsync_Found_Should_Return_Task (dòng 71):
using (var seedContext = Factory.CreateDbContext())
{
    ...
    await context.SaveChangesAsync();   // ← 'context' không được khai báo trong scope này
}

// TRONG test GetByIdAsync_IsDeleted_Should_Return_Null (dòng 132):
using (var seedContext = Factory.CreateDbContext())
{
    ...
    await context.SaveChangesAsync();   // ← lỗi tương tự
}
```

**Cách fix:** Đổi `context` → `seedContext` tại cả hai chỗ.

---

### BUG-03 [CRITICAL] — `TaskRepositoryTests` dùng property `ExecutablePath` không tồn tại

**File:** `Tests/TaskScheduler.Infrastructure.Tests/Persistence/TaskRepositoryTests.cs:91`

**Vấn đề:**
```csharp
result.ExecutablePath.Should().Be("backup.exe");   // ← property 'ExecutablePath' không tồn tại
```

Entity `ScheduledTask` dùng property tên `Command`, không phải `ExecutablePath`.

**Cách fix:**
```csharp
result.Command.Should().Be("backup.exe");
```

---

### BUG-04 [CRITICAL] — `TaskRepositoryTests.UpdateAsync_Should_Change_Task` gọi `MarkAsFailed()` thiếu argument

**File:** `Tests/TaskScheduler.Infrastructure.Tests/Persistence/TaskRepositoryTests.cs:167`

**Vấn đề:**
```csharp
task.MarkAsFailed();   // ← method signature là MarkAsFailed(string reason), bắt buộc có argument
```

**Cách fix:**
```csharp
task.MarkAsFailed("Test failure reason");
```

---

### BUG-05 [CRITICAL] — `DeleteTaskCommandHandlerTests` khởi tạo với object initializer trên private setter

**File:** `Tests/TaskScheduler.Application.Tests/Tasks/Commands/DeleteTask/DeleteTaskCommandHandlerTests.cs:44-46`

**Vấn đề:**
```csharp
var deletedTask = new ScheduledTask(...)
{
    IsDeleted = true   // ← IsDeleted có private setter, compile error
};
```

`ScheduledTask.IsDeleted` có `private set` nên không thể gán từ bên ngoài bằng object initializer.

**Cách fix:**
```csharp
var deletedTask = new ScheduledTask(...);
deletedTask.SoftDelete();   // ← dùng method của entity
```

---

### BUG-06 [CRITICAL] — `DeleteTaskCommandHandlerTests.Handle_ShouldUnscheduleTask` verify sai signature

**File:** `Tests/TaskScheduler.Application.Tests/Tasks/Commands/DeleteTask/DeleteTaskCommandHandlerTests.cs:136`

**Vấn đề:**
```csharp
schedulerMock.Verify(x => x.UnscheduleTaskAsync(existingTask), Times.Once);
// ISchedulerService.UnscheduleTaskAsync(Guid taskId) — nhận Guid
// nhưng đang truyền vào ScheduledTask object
```

**Cách fix:**
```csharp
schedulerMock.Verify(x => x.UnscheduleTaskAsync(existingTask.Id), Times.Once);
```

---

### BUG-07 [CRITICAL] — `PauseTaskCommandHandlerTests` khởi tạo handler sai

**File:** `Tests/TaskScheduler.Application.Tests/Tasks/Commands/PauseTask/PauseTaskCommandHandlerTests.cs:52`

**Vấn đề:**
```csharp
var handler = new PauseTaskCommand(repoMock.Object, Mock.Of<ISchedulerService>());
// ← dùng nhầm PauseTaskCommand thay vì PauseTaskHandler
```

**Cách fix:**
```csharp
var handler = new PauseTaskHandler(repoMock.Object, Mock.Of<ISchedulerService>());
```

---

### BUG-08 [CRITICAL] — `PauseTaskCommandHandlerTests` set property `Status` và `IsDeleted` qua object initializer

**File:** `Tests/TaskScheduler.Application.Tests/Tasks/Commands/PauseTask/PauseTaskCommandHandlerTests.cs:44-46, 76-77, 102-103, 130-131, 160-161`

**Vấn đề:** Nhiều chỗ trong file này gán `Status` và `IsDeleted` bằng object initializer — cả hai đều có `private set`.

```csharp
// Sai (compile error)
var task = new ScheduledTask(...) { IsDeleted = true };
var task = new ScheduledTask(...) { Status = ScheduledTaskStatus.Active };
var task = new ScheduledTask(...) { Status = "Pending" };
```

**Cách fix:**
- `IsDeleted = true` → gọi `task.SoftDelete()`
- `Status = ScheduledTaskStatus.Active` → gọi `task.MarkAsActive()`
- `Status = ScheduledTaskStatus.Running` → gọi `task.MarkAsRunning()`
- `Status = ScheduledTaskStatus.Completed` → gọi `task.MarkAsCompleted()`
- `Status = "Pending"` (string) → không cần set, `Pending` là status mặc định sau constructor
- `Status = "Running"` → gọi `task.MarkAsRunning()`
- `Status = "Completed"` → gọi `task.MarkAsCompleted()`

---

### BUG-09 [CRITICAL] — `PauseTaskCommandHandlerTests.Handle_ShouldUnscheduleTask` verify sai signature

**File:** `Tests/TaskScheduler.Application.Tests/Tasks/Commands/PauseTask/PauseTaskCommandHandlerTests.cs:173`

**Vấn đề:** Tương tự BUG-06 — `UnscheduleTaskAsync` nhận `Guid` nhưng đang truyền `ScheduledTask`.

**Cách fix:**
```csharp
schedulerMock.Verify(x => x.UnscheduleTaskAsync(existingTask.Id), Times.Once);
```

---

### BUG-10 [CRITICAL] — `HangfireSchedulerServiceTests` khởi tạo `ScheduledTask` sai

**File:** `Tests/TaskScheduler.Infrastructure.Tests/Scheduling/HangfireSchedulerServiceTests.cs:27-32`

**Vấn đề:**
```csharp
var task = new ScheduledTask
{
    Id = Guid.NewGuid(),
    Name = "Backup",
    CronExpression = "0 * * * *"   // ← object initializer, nhưng constructor của ScheduledTask yêu cầu tham số
};
```

`ScheduledTask` chỉ có constructor parameterized (private constructor cho EF Core không dùng được từ test). Ngoài ra `CronExpression` là `ValueObject`, không thể gán `string` trực tiếp.

**Cách fix:**
```csharp
var task = new ScheduledTask(
    "Backup",
    "Daily backup",
    "0 * * * *",     // ← constructor nhận string, tự tạo CronExpression bên trong
    "backup.exe",
    3
);
```

Và sửa assertion:
```csharp
job.Cron.Should().Be(task.CronExpression.Value);   // ← .Value để lấy string từ Value Object
```

---

### BUG-11 [CRITICAL] — `TokenServiceTests` gọi `GenerateJwtToken` với `string` nhưng method nhận `User`

**File:** `Tests/TaskScheduler.Infrastructure.Tests/Services/TokenServiceTests.cs:103,124`

**Vấn đề:**
```csharp
var token = _tokenService.GenerateJwtToken(user);   // 'user' là string "admin"
```

`ITokenService.GenerateJwtToken` nhận `User` entity, không phải `string`.

**Cách fix:** Tạo `User` object để truyền vào:
```csharp
var user = new User
{
    Id = Guid.NewGuid(),
    Username = "admin",
    Email = "admin@example.com",
    IsActive = true
};
var token = _tokenService.GenerateJwtToken(user);
```

Và cập nhật assertion claim cho đúng với implementation thực tế của `TokenService.GenerateJwtToken`.

---

### BUG-12 [HIGH] — `ExecutionLogRepositoryTests` hai test trùng tên

**File:** `Tests/TaskScheduler.Infrastructure.Tests/Persistence/ExecutionLogRepositoryTests.cs:52,147`

**Vấn đề:** Hai method đều tên `GetByTaskIdAsync_Found_Should_Return_List_ExecutionLog` — compile error.

**Cách fix:** Đổi tên test ở dòng 147 thành:
```csharp
public async Task GetDetailsAsync_Found_Should_Return_ExecutionLog()
```

---

### BUG-13 [HIGH] — `ExecutionLogRepositoryTests` khởi tạo `TaskExecutionLog` sai constructor

**File:** `Tests/TaskScheduler.Infrastructure.Tests/Persistence/ExecutionLogRepositoryTests.cs:22-26,57-61,64-68...`

**Vấn đề:**
```csharp
var task = new TaskExecutionLog(
    Guid.NewGuid(),    // logId?
    Guid.NewGuid(),    // taskId?
    DateTime.UtcNow,
    "Success"
);
```

Cần kiểm tra constructor thực tế của `TaskExecutionLog` trong `src/TaskScheduler.Domain/Entities/TaskExecutionLog.cs` và căn chỉnh tham số cho đúng.

---

## NHÓM 2 — Logic gap trong production code

### GAP-01 [HIGH] — `HangfireSchedulerService.RescheduleTaskAsync` trùng lặp 100% với `ScheduleTaskAsync`

**File:** `src/TaskScheduler.Infrastructure/Scheduling/HangfireSchedulerService.cs:30-38`

**Vấn đề:** Hai method làm hệt nhau. Nếu logic thay đổi chỉ sửa một chỗ sẽ diverge.

**Cách fix:**
```csharp
public Task RescheduleTaskAsync(ScheduledTask task)
{
    return ScheduleTaskAsync(task);   // delegate
}
```

---

### GAP-02 [HIGH] — `TriggerTaskHandler` không kiểm tra status trước khi trigger

**File:** `src/TaskScheduler.Application/Tasks/Commands/TriggerTask/TriggerTaskHandler.cs`

**Vấn đề:** Task có status bất kỳ (`Completed`, `Paused`, `Running`) đều có thể bị trigger thủ công. Không có guard.

**Cách fix:** Thêm check status trước khi gọi `TriggerNow`:
```csharp
if (task.Status != ScheduledTaskStatus.Active && task.Status != ScheduledTaskStatus.Failed)
    throw new InvalidOperationException("Only Active or Failed tasks can be triggered manually.");
```

---

### GAP-03 [MEDIUM] — `TaskExecutionService` dùng `cmd.exe` hardcode — chỉ chạy trên Windows

**File:** `src/TaskScheduler.Infrastructure/Scheduling/TaskExecutionService.cs:137`

**Vấn đề:**
```csharp
FileName = "cmd.exe",
Arguments = $"/c {task.Command}",
```

Hardcode `cmd.exe` khiến code không chạy được trên Linux/macOS (server production thường là Linux).

**Cách fix:**
```csharp
bool isWindows = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
    System.Runtime.InteropServices.OSPlatform.Windows);

process.StartInfo = new ProcessStartInfo
{
    FileName = isWindows ? "cmd.exe" : "/bin/sh",
    Arguments = isWindows ? $"/c {task.Command}" : $"-c \"{task.Command}\"",
    ...
};
```

---

### GAP-04 [MEDIUM] — `ExecutionLogRepositoryTests` tạo log với `logId` là `TaskId` — semantic sai

**File:** `Tests/TaskScheduler.Infrastructure.Tests/Persistence/ExecutionLogRepositoryTests.cs:150`

**Vấn đề:**
```csharp
var logId = Guid.NewGuid();
var task = new TaskExecutionLog(
    logId,           // ← truyền logId vào tham số đầu tiên
    Guid.NewGuid(),  // ← tham số thứ hai
    ...
);
```

Cần xác nhận thứ tự tham số của `TaskExecutionLog` constructor để đảm bảo `logId` và `taskId` không bị hoán đổi.

---

## NHÓM 3 — Missing test coverage

### TEST-01 [MEDIUM] — `UserRepositoryTests` thiếu `GetByEmailAsync` implementation

**File:** `Tests/TaskScheduler.Infrastructure.Tests/Persistence/UserRepositoryTests.cs:51`

Test đang gọi `repository.GetByEmailAsync(...)`. Cần kiểm tra `UserRepository` có implement method này không. Nếu không có → thêm vào `IUserRepository` + `UserRepository`.

---

### TEST-02 [MEDIUM] — Không có test cho `TriggerTaskCommandHandler`

**File cần tạo (đã có file):** `Tests/TaskScheduler.Application.Tests/Tasks/Commands/TriggerTask/TriggerTaskCommandHandlerTests.cs`

Kiểm tra file này đã có test chưa. Nếu chưa, cần thêm các case:
- Task không tồn tại → `KeyNotFoundException`
- Task status `Completed` → `InvalidOperationException` (sau khi fix GAP-02)
- Task status `Active` → `ITaskExecutionService.TriggerNow` được gọi

---

### TEST-03 [MEDIUM] — `LoggingBehaviorTests` và `PerformanceBehaviorTests` có thể còn rỗng

**File:** `Tests/TaskScheduler.Application.Tests/Behaviors/LoggingBehaviorTests.cs`  
**File:** `Tests/TaskScheduler.Application.Tests/Behaviors/PerformanceBehaviorTests.cs`

Cần mở hai file này để kiểm tra có test thực sự bên trong không. Nếu rỗng, thêm ít nhất:
- `LoggingBehavior`: mock `ILogger`, verify log được ghi khi request đến và response về
- `PerformanceBehavior`: test với handler giả chạy chậm, verify warning được log khi vượt ngưỡng

---

### TEST-04 [LOW] — `ExecutionLogRepositoryTests` dùng sai data type cho `ErrorMessage` field

**File:** `Tests/TaskScheduler.Infrastructure.Tests/Persistence/ExecutionLogRepositoryTests.cs:26`

```csharp
"Success"   // ← đang truyền vào vị trí ErrorMessage
```

`ErrorMessage` thường là `null` khi success. Cần kiểm tra constructor `TaskExecutionLog` để xác định field nào là gì.

---

### TEST-05 [LOW] — Thiếu API Integration Test cho các action endpoint

**File:** `Tests/TaskScheduler.Api.Tests/Controllers/TasksControllerTests.cs`

Hiện tại đã có các test CRUD cơ bản. Còn thiếu test cho:
- `POST /api/v1/tasks/{id}/activate` — kích hoạt task
- `POST /api/v1/tasks/{id}/pause` — tạm dừng
- `POST /api/v1/tasks/{id}/resume` — tiếp tục
- `POST /api/v1/tasks/{id}/trigger` — chạy thủ công
- `GET /api/v1/tasks/{id}/logs` — xem lịch sử execution

---

## NHÓM 4 — Nice-to-have (làm sau khi toàn bộ test pass)

### NICE-01 [LOW] — Rate Limiting cho `/trigger` endpoint

**File:** `src/TaskScheduler.Api/Program.cs`

.NET 8+ có built-in `AddRateLimiter`. Giới hạn `/trigger` tối đa N request/phút per user để tránh spam.

---

### NICE-02 [LOW] — API Versioning chính thức

Route đang có `/v1/` nhưng chưa dùng `Asp.Versioning` package. Cần setup nếu muốn hỗ trợ v2 sau này.

---

### NICE-03 [LOW] — Email/Webhook notification khi task fail

`TaskFailedEvent` đã được raise trong entity. Cần tạo handler lắng nghe event này và gọi `IEmailService` (interface đã có sẵn tại `src/TaskScheduler.Application/Interfaces/IEmailService.cs`).

---

## Thứ tự thực hiện

```
Tuần này (unblock tests):
  BUG-01 → BUG-13 (fix theo thứ tự, chạy dotnet test sau mỗi fix)

Sau đó:
  GAP-01 → GAP-04 (production code)

Tuần sau:
  TEST-01 → TEST-05 (hoàn thiện coverage)

Sau cùng:
  NICE-01 → NICE-03 (nâng cao)
```

---

## Lệnh kiểm tra nhanh

```powershell
# Build — xem compile error
dotnet build Task-scheduler-system.sln

# Chạy toàn bộ test
dotnet test Task-scheduler-system.sln --logger "console;verbosity=minimal"

# Chạy riêng từng project test
dotnet test Tests/TaskScheduler.Domain.Tests/TaskScheduler.Domain.Tests.csproj
dotnet test Tests/TaskScheduler.Application.Tests/TaskScheduler.Application.Tests.csproj
dotnet test Tests/TaskScheduler.Infrastructure.Tests/TaskScheduler.Infrastructure.Tests.csproj
dotnet test Tests/TaskScheduler.Api.Tests/TaskScheduler.Api.Tests.csproj
```
