# WORKLOG — Việc còn lại cần làm

> Ngày cập nhật: 2026-08-03  
> Dựa trên: đọc trực tiếp source code hiện tại (không phải TODO/REVIEW cũ)  
> Trạng thái baseline test (cần chạy lại để xác nhận):  
> `dotnet test Task-scheduler-system.sln --logger "console;verbosity=minimal"`

---

## Mức độ ưu tiên

- **[HIGH]** — Bug thực sự, ảnh hưởng runtime hoặc testability
- **[MEDIUM]** — Tính năng còn stub, chưa có implementation
- **[LOW]** — Code quality, test coverage bổ sung

---

## NHÓM 1 — Bug cần fix

### BUG-01 [HIGH] — `ScheduleRetry` dùng static Hangfire API

**File:** `src/TaskScheduler.Infrastructure/Scheduling/TaskExecutionService.cs` — khoảng dòng 206

**Vấn đề:** `ScheduleRetry` gọi `BackgroundJob.Schedule<ITaskExecutionService>(...)` — static API không mock được,
trong khi `TriggerNow` ở dòng 115 đã dùng đúng `_backgroundJobClient.Enqueue<TaskExecutionService>(...)`.
Hệ quả: không thể unit test nhánh retry; inconsistent DI.

**Fix:**
```csharp
// Thay dòng static:
BackgroundJob.Schedule<ITaskExecutionService>(...);

// Bằng injected client:
_backgroundJobClient.Schedule<ITaskExecutionService>(
    x => x.ExecuteTask(task.Id),
    delay
);
```

---

### BUG-02 [HIGH] — `SmsMessage` có property private và lowercase

**File:** `src/TaskScheduler.Application/Common/Models/SmsMessage.cs`

**Vấn đề:** Các property thiếu `public` và không theo PascalCase — class không dùng được như DTO.

**Fix:**
```csharp
// TRƯỚC (sai)
string phoneNumber { get; init; }
string message { get; init; }

// SAU (đúng)
public string PhoneNumber { get; init; }
public string Message { get; init; }
```

---

### BUG-03 [MEDIUM] — `app.UseRateLimiter()` bị gọi 2 lần

**File:** `src/TaskScheduler.Api/Program.cs` — khoảng dòng 51 và 54

**Vấn đề:** Middleware `UseRateLimiter()` được đăng ký hai lần liên tiếp — thừa, có thể gây double-processing.

**Fix:** Xóa một trong hai lần gọi.

---

### BUG-04 [LOW] — Duplicate `using MediatR` trong `LoggingBehavior.cs`

**File:** `src/TaskScheduler.Application/Common/Behaviors/LoggingBehavior.cs` — dòng 5 và 8

**Fix:** Xóa một trong hai `using MediatR;`. Dùng IDE "Remove Unnecessary Usings".

---

## NHÓM 2 — Event Handlers còn stub

> Thứ tự làm từ trên xuống — mỗi bước độc lập, không cần làm theo thứ tự nghiêm ngặt.

### EH-01 [MEDIUM] — Fix `TaskCompletedLogHandler` (thay Console.WriteLine)

**File:** `src/TaskScheduler.Application/EventHandlers/Logging/TaskCompletedLogHandler.cs`

Handler đã implement interface đúng nhưng dùng `Console.WriteLine` thay vì `ILogger`.

**Fix:**
```csharp
public class TaskCompletedLogHandler : INotificationHandler<DomainEventNotification<TaskCompletedEvent>>
{
    private readonly ILogger<TaskCompletedLogHandler> _logger;

    public TaskCompletedLogHandler(ILogger<TaskCompletedLogHandler> logger)
        => _logger = logger;

    public Task Handle(DomainEventNotification<TaskCompletedEvent> notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Task completed: {TaskId}", notification.DomainEvent.TaskId);
        return Task.CompletedTask;
    }
}
```

Không cần đăng ký DI thêm — MediatR scan assembly tự động.

---

### EH-02 [MEDIUM] — Implement `TaskFailedLogHandler`

**File:** `src/TaskScheduler.Application/EventHandlers/Logging/TaskFailedLogHandler.cs`

Hiện là class rỗng. Không cần service ngoài, chỉ cần `ILogger`.

**Implementation:**
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
            => _logger = logger;

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

---

### EH-03 [MEDIUM] — Implement Metrics Handlers + `MetricsService`

**Files cần làm (theo thứ tự):**

**Bước A** — Implement `TaskCompletedMetricsHandler`:

```csharp
// src/TaskScheduler.Application/EventHandlers/Metrics/TaskCompletedMetricsHandler.cs
public class TaskCompletedMetricsHandler : INotificationHandler<DomainEventNotification<TaskCompletedEvent>>
{
    private readonly IMetricsService _metrics;
    public TaskCompletedMetricsHandler(IMetricsService metrics) => _metrics = metrics;

    public Task Handle(DomainEventNotification<TaskCompletedEvent> notification, CancellationToken cancellationToken)
        => _metrics.IncrementCompletedTasksAsync();
}
```

**Bước B** — Implement `TaskFailedMetricsHandler`:

```csharp
// src/TaskScheduler.Application/EventHandlers/Metrics/TaskFailedMetricsHandler.cs
public class TaskFailedMetricsHandler : INotificationHandler<DomainEventNotification<TaskFailedEvent>>
{
    private readonly IMetricsService _metrics;
    public TaskFailedMetricsHandler(IMetricsService metrics) => _metrics = metrics;

    public Task Handle(DomainEventNotification<TaskFailedEvent> notification, CancellationToken cancellationToken)
        => _metrics.IncrementFailedTasksAsync();
}
```

**Bước C** — Tạo `MetricsService` trong Infrastructure:

```csharp
// src/TaskScheduler.Infrastructure/Services/MetricsService.cs
using TaskScheduler.Application.Interfaces;

namespace TaskScheduler.Infrastructure.Services
{
    public class MetricsService : IMetricsService
    {
        // TODO: thay bằng Prometheus Counter hoặc Application Insights metric
        public Task IncrementCompletedTasksAsync() => Task.CompletedTask;
        public Task IncrementFailedTasksAsync() => Task.CompletedTask;
    }
}
```

**Bước D** — Đăng ký DI trong `src/TaskScheduler.Infrastructure/DependencyInjection.cs`:

```csharp
services.AddScoped<IMetricsService, MetricsService>();
```

---

### EH-04 [MEDIUM] — Implement `SendEmailHandler` + `SmtpEmailService`

**Phụ thuộc:** BUG-02 (SmsMessage fix) không liên quan trực tiếp nhưng nên fix trước.

**Bước A** — Implement handler:

```csharp
// src/TaskScheduler.Application/EventHandlers/Notifications/SendEmailHandler.cs
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
        public SendEmailHandler(IEmailService emailService) => _emailService = emailService;

        public Task Handle(DomainEventNotification<TaskFailedEvent> notification, CancellationToken cancellationToken)
        {
            var ev = notification.DomainEvent;
            var message = new EmailMessage
            {
                To = "admin@example.com",   // TODO: lấy từ config hoặc task settings
                Subject = $"Task {ev.TaskId} failed",
                Body = $"Task {ev.TaskId} failed. Reason: {ev.Reason}"
            };
            return _emailService.SendEmailAsync(message);
        }
    }
}
```

**Bước B** — Tạo `SmtpEmailService` trong Infrastructure:

```csharp
// src/TaskScheduler.Infrastructure/Services/SmtpEmailService.cs
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
        public SmtpEmailService(IConfiguration config) => _config = config;

        public async Task SendEmailAsync(EmailMessage message)
        {
            var host = _config["Smtp:Host"]!;
            var port = int.Parse(_config["Smtp:Port"] ?? "587");
            var user = _config["Smtp:User"]!;
            var pass = _config["Smtp:Password"]!;

            using var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(user, pass),
                EnableSsl = true
            };
            await client.SendMailAsync(new MailMessage(user, message.To, message.Subject, message.Body));
        }
    }
}
```

**Bước C** — Thêm vào `appsettings.json`:

```json
"Smtp": {
  "Host": "smtp.gmail.com",
  "Port": "587",
  "User": "your@email.com",
  "Password": "your-app-password"
}
```

**Bước D** — Đăng ký DI trong `Infrastructure/DependencyInjection.cs`:

```csharp
services.AddScoped<IEmailService, SmtpEmailService>();
```

---

### EH-05 [LOW] — `SendSmsHandler` (tùy chọn, defer nếu chưa có SMS provider)

**File:** `src/TaskScheduler.Application/EventHandlers/Notifications/SendSmsHandler.cs`

Pattern giống `SendEmailHandler`. Cần provider SMS thực tế (Twilio, v.v.).  
**Khuyến nghị:** Bỏ qua cho đến khi có yêu cầu cụ thể về SMS provider.

---

## NHÓM 3 — Test coverage bổ sung

### TEST-01 [LOW] — Viết test cho EventHandlers sau khi implement (NHÓM 2)

**Files trống cần viết sau khi handlers được implement:**

- `Tests/TaskScheduler.Infrastructure.Tests/EventHandlers/TaskCompletedEventHandlerTests.cs`
- `Tests/TaskScheduler.Infrastructure.Tests/EventHandlers/TaskCreatedEventHandlerTests.cs`

**Pattern cần follow** (xem `TaskJobTests.cs`):
1. Mock service phụ thuộc (`IMetricsService`, `IEmailService`, v.v.)
2. Gọi `handler.Handle(notification, CancellationToken.None)`
3. Verify service method được gọi đúng 1 lần với đúng argument

---

### TEST-02 [LOW] — Verify test API-02 pass

**Lệnh:**
```powershell
dotnet test --filter "GetTasks_WithoutToken_Should_Return_Unauthorized"
```

Nếu nhận được 400 thay vì 401: kiểm tra thứ tự middleware trong `Program.cs`.  
`UseAuthentication()` phải đứng trước hoặc `ExceptionHandlingMiddleware` phải handle `UnauthorizedAccessException` → 401 (hiện đã có handler này).

---

## NHÓM 4 — Tính năng nâng cao (future)

### FEAT-01 [LOW] — MetricsService thực tế với Prometheus

**Khi nào làm:** Sau khi EH-03 có skeleton chạy được.

```powershell
dotnet add package prometheus-net.AspNetCore --project src/TaskScheduler.Infrastructure
```

Thay `Task.CompletedTask` trong `MetricsService` bằng `Counter.Labels(...).Inc()`.

---

### FEAT-02 [LOW] — Email recipient động (từ task config hoặc user settings)

**Hiện trạng:** `SendEmailHandler` hardcode `"admin@example.com"`.  
**Hướng:** Thêm field `NotifyEmail` vào `ScheduledTask` entity, đọc từ đó khi gửi.

---

## Thứ tự làm đề xuất

```
Ưu tiên cao — làm trước:
  1. BUG-01  Fix ScheduleRetry dùng _backgroundJobClient
             File: src/.../Scheduling/TaskExecutionService.cs ~dòng 206

  2. BUG-02  Fix SmsMessage property public + PascalCase
             File: src/.../Common/Models/SmsMessage.cs

  3. BUG-03  Xóa UseRateLimiter() thừa
             File: src/TaskScheduler.Api/Program.cs

Ưu tiên trung bình — làm theo nhóm:
  4. EH-01   Fix TaskCompletedLogHandler (ILogger thay Console.WriteLine)
  5. EH-02   Implement TaskFailedLogHandler
  6. EH-03   Implement Metrics Handlers + MetricsService + đăng ký DI
  7. EH-04   Implement SendEmailHandler + SmtpEmailService + đăng ký DI

Ưu tiên thấp — làm sau:
  8. TEST-01 Viết EventHandler tests (sau khi EH-01..04 xong)
  9. TEST-02 Verify API-02 unauthorized test pass
 10. BUG-04  Remove duplicate using MediatR trong LoggingBehavior.cs
 11. EH-05   SendSmsHandler (khi có SMS provider)
 12. FEAT-01 MetricsService thực tế với Prometheus
 13. FEAT-02 Email recipient động từ task settings
```

---

## Lệnh kiểm tra

```powershell
# Baseline — chạy toàn bộ
dotnet test Task-scheduler-system.sln --logger "console;verbosity=minimal"

# Từng project
dotnet test Tests/TaskScheduler.Domain.Tests/TaskScheduler.Domain.Tests.csproj
dotnet test Tests/TaskScheduler.Application.Tests/TaskScheduler.Application.Tests.csproj
dotnet test Tests/TaskScheduler.Infrastructure.Tests/TaskScheduler.Infrastructure.Tests.csproj
dotnet test Tests/TaskScheduler.Api.Tests/TaskScheduler.Api.Tests.csproj

# Test cụ thể
dotnet test --filter "GetTasks_WithoutToken_Should_Return_Unauthorized"
```

---

## NHÓM 5 — Docker, CI/CD, Deploy

> Hiện trạng: chỉ có `docker-compose.yml` chạy PostgreSQL. Không có Dockerfile, không có CI/CD, không có cấu hình deploy.

---

### DOCKER-01 [HIGH] — Tạo Dockerfile cho API

**File cần tạo:** `Dockerfile` (root của repo, cạnh `.sln`)

Dùng multi-stage build để image production nhỏ gọn:

```dockerfile
# Stage 1 — Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Task-scheduler-system.sln .
COPY src/TaskScheduler.Domain/TaskScheduler.Domain.csproj             src/TaskScheduler.Domain/
COPY src/TaskScheduler.Application/TaskScheduler.Application.csproj   src/TaskScheduler.Application/
COPY src/TaskScheduler.Infrastructure/TaskScheduler.Infrastructure.csproj src/TaskScheduler.Infrastructure/
COPY src/TaskScheduler.Api/TaskScheduler.Api.csproj                   src/TaskScheduler.Api/

RUN dotnet restore src/TaskScheduler.Api/TaskScheduler.Api.csproj

COPY . .
RUN dotnet publish src/TaskScheduler.Api/TaskScheduler.Api.csproj \
    -c Release -o /app/publish --no-restore

# Stage 2 — Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "TaskScheduler.Api.dll"]
```

---

### DOCKER-02 [HIGH] — Tạo `.dockerignore`

**File cần tạo:** `.dockerignore` (root của repo)

```
**/bin/
**/obj/
**/.vs/
**/.vscode/
**/.idea/
**/logs/
*.user
*.suo
.git/
Docs/
Tests/
```

---

### DOCKER-03 [HIGH] — Cập nhật `docker-compose.yml`

**File:** `docker-compose.yml`

Vấn đề hiện tại:
- Chỉ có PostgreSQL, không có service cho API
- Không có volume — dữ liệu mất khi container restart
- DB name mismatch: compose dùng `tasks`, connection string dùng `task_scheduler`
- Password hardcode plaintext

**File mới:**

```yaml
version: '3.9'

services:
  postgres:
    image: postgres:18
    environment:
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: ${DB_PASSWORD}
      POSTGRES_DB: task_scheduler
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres -d task_scheduler"]
      interval: 10s
      timeout: 5s
      retries: 5

  api:
    build:
      context: .
      dockerfile: Dockerfile
    ports:
      - "8080:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=task_scheduler;Username=postgres;Password=${DB_PASSWORD}
      - Jwt__Secret=${JWT_SECRET}
      - Jwt__Issuer=TaskSchedulerAPI
      - Jwt__Audience=TaskSchedulerClient
      - Smtp__Host=${SMTP_HOST}
      - Smtp__Port=${SMTP_PORT}
      - Smtp__User=${SMTP_USER}
      - Smtp__Password=${SMTP_PASSWORD}
    depends_on:
      postgres:
        condition: service_healthy

volumes:
  postgres_data:
```

---

### DOCKER-04 [HIGH] — Tạo file `.env` (local) và `.env.example` (commit lên git)

**File `.env`** (thêm vào `.gitignore` — KHÔNG commit):

```env
DB_PASSWORD=quanph123
JWT_SECRET=MokV88kdPYRajT/E91TnLwyfor8HiKkvbS6BxeXUD93uUaxh7qGeS6KlWcD4GmXPgrgeDF61uritHkn7OgrFOA==
SMTP_HOST=smtp.gmail.com
SMTP_PORT=587
SMTP_USER=your@email.com
SMTP_PASSWORD=your-app-password
```

**File `.env.example`** (commit lên git — chỉ chứa key, không có value thật):

```env
DB_PASSWORD=
JWT_SECRET=
SMTP_HOST=
SMTP_PORT=
SMTP_USER=
SMTP_PASSWORD=
```

**Thêm vào `.gitignore`:**

```
.env
```

---

### DOCKER-05 [HIGH] — Xóa secrets khỏi `appsettings.json`

**File:** `src/TaskScheduler.Api/appsettings.json`

JWT secret và DB password đang hardcode và đã được commit vào git — đây là security risk nghiêm trọng.

**Cách fix:**

1. Xóa giá trị thật, để placeholder:

```json
"ConnectionStrings": {
    "DefaultConnection": ""
},
"Jwt": {
    "Secret": "",
    "Issuer": "TaskSchedulerAPI",
    "Audience": "TaskSchedulerClient",
    "ExpireHours": 24
}
```

2. Đưa giá trị thật vào `appsettings.Development.json` (đã gitignore) cho local dev.

3. Trong production (Docker/CI): inject qua environment variable — ASP.NET Core tự map `ConnectionStrings__DefaultConnection` → `ConnectionStrings:DefaultConnection`.

> **Lưu ý bảo mật:** Secret đã bị lộ trong git history. Sau khi fix code, cần rotate (tạo JWT secret mới, đổi DB password).

---

### CICD-01 [HIGH] — Tạo GitHub Actions workflow: CI (build + test)

**File cần tạo:** `.github/workflows/ci.yml`

```yaml
name: CI

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]

jobs:
  build-and-test:
    runs-on: ubuntu-latest

    services:
      postgres:
        image: postgres:18
        env:
          POSTGRES_USER: postgres
          POSTGRES_PASSWORD: test_password
          POSTGRES_DB: task_scheduler
        ports:
          - 5432:5432
        options: >-
          --health-cmd pg_isready
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5

    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Setup .NET 10
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Restore dependencies
        run: dotnet restore Task-scheduler-system.sln

      - name: Build
        run: dotnet build Task-scheduler-system.sln --no-restore -c Release

      - name: Run tests
        run: dotnet test Task-scheduler-system.sln --no-build -c Release \
             --logger "trx;LogFileName=results.xml" \
             --results-directory ./TestResults
        env:
          ConnectionStrings__DefaultConnection: "Host=localhost;Port=5432;Database=task_scheduler;Username=postgres;Password=test_password"
          Jwt__Secret: "ci-test-secret-at-least-32-chars-long-for-hmac"
          Jwt__Issuer: "TaskSchedulerAPI"
          Jwt__Audience: "TaskSchedulerClient"
          ASPNETCORE_ENVIRONMENT: Testing

      - name: Upload test results
        uses: actions/upload-artifact@v4
        if: always()
        with:
          name: test-results
          path: ./TestResults/*.xml
```

---

### CICD-02 [MEDIUM] — Tạo GitHub Actions workflow: CD (build + push Docker image)

**File cần tạo:** `.github/workflows/cd.yml`

Trigger khi push lên `main` (sau khi CI pass). Build Docker image và push lên GitHub Container Registry (GHCR) — miễn phí với public repo.

```yaml
name: CD

on:
  push:
    branches: [main]

jobs:
  docker:
    runs-on: ubuntu-latest
    needs: []   # thêm job CI vào đây nếu muốn CD chạy sau CI

    permissions:
      contents: read
      packages: write

    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Log in to GitHub Container Registry
        uses: docker/login-action@v3
        with:
          registry: ghcr.io
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}

      - name: Build and push Docker image
        uses: docker/build-push-action@v5
        with:
          context: .
          push: true
          tags: |
            ghcr.io/${{ github.repository_owner }}/task-scheduler-api:latest
            ghcr.io/${{ github.repository_owner }}/task-scheduler-api:${{ github.sha }}
```

**Lưu ý:** `GITHUB_TOKEN` có sẵn tự động trong GitHub Actions, không cần tạo secret thủ công.

---

### CICD-03 [LOW] — Thêm GitHub Actions secrets cho production deploy

Vào **GitHub repo → Settings → Secrets and variables → Actions**, thêm:

| Secret name | Giá trị |
|---|---|
| `DB_PASSWORD` | Password PostgreSQL production |
| `JWT_SECRET` | JWT secret mới (sau khi rotate) |
| `SMTP_HOST` | SMTP host |
| `SMTP_USER` | SMTP user |
| `SMTP_PASSWORD` | SMTP password / app password |

---

### DEPLOY-01 [MEDIUM] — Chạy EF Core migration tự động khi startup

**Vấn đề:** Hiện tại migration phải chạy thủ công bằng `dotnet ef database update`. Trong môi trường container/CI, cần chạy tự động.

**Cách làm:** Thêm vào `Program.cs` sau khi `app` được build, trước `app.Run()`:

```csharp
// Tự động migrate khi startup (chỉ nếu không phải môi trường Testing)
if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
}
```

**Lưu ý:** Cách này phù hợp cho project nhỏ/vừa. Project lớn nên dùng migration job riêng biệt trước khi deploy.

---

### DEPLOY-02 [MEDIUM] — Tạo `docker-compose.override.yml` cho local dev

Dùng khi chạy local — override để expose thêm port debug, mount source code, v.v.

**File cần tạo:** `docker-compose.override.yml`

```yaml
version: '3.9'

services:
  api:
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
    volumes:
      - ./src:/src   # hot-reload khi code thay đổi (dùng với dotnet watch)
```

---

### DEPLOY-03 [LOW] — Healthcheck endpoint cho API

**Mục đích:** Docker, load balancer, và Kubernetes dùng để biết API đã sẵn sàng nhận request chưa.

**Cách làm:** Thêm vào `Program.cs`:

```csharp
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!);

// ...

app.MapHealthChecks("/health");
```

Cài package:

```powershell
dotnet add package AspNetCore.HealthChecks.NpgSql --project src/TaskScheduler.Api
```

---

## Thứ tự làm đề xuất (cập nhật đầy đủ)

```
Ưu tiên cao — làm trước:
  1.  BUG-01     Fix ScheduleRetry dùng _backgroundJobClient
  2.  BUG-02     Fix SmsMessage property public + PascalCase
  3.  BUG-03     Xóa UseRateLimiter() thừa trong Program.cs
  4.  DOCKER-05  Xóa secrets khỏi appsettings.json → dùng env var
  5.  DOCKER-04  Tạo .env + .env.example, thêm .env vào .gitignore
  6.  DOCKER-01  Tạo Dockerfile (multi-stage build)
  7.  DOCKER-02  Tạo .dockerignore
  8.  DOCKER-03  Cập nhật docker-compose.yml (thêm api service + volume + healthcheck)

Ưu tiên trung bình:
  9.  CICD-01    Tạo .github/workflows/ci.yml (build + test tự động)
 10.  CICD-02    Tạo .github/workflows/cd.yml (build + push Docker image)
 11.  CICD-03    Thêm secrets vào GitHub Actions
 12.  DEPLOY-01  Auto migrate EF Core khi startup
 13.  EH-01      Fix TaskCompletedLogHandler (ILogger thay Console.WriteLine)
 14.  EH-02      Implement TaskFailedLogHandler
 15.  EH-03      Implement Metrics Handlers + MetricsService + đăng ký DI
 16.  EH-04      Implement SendEmailHandler + SmtpEmailService + đăng ký DI

Ưu tiên thấp — làm sau:
 17.  DEPLOY-02  docker-compose.override.yml cho local dev
 18.  DEPLOY-03  Healthcheck endpoint /health
 19.  TEST-01    Viết EventHandler tests (sau khi EH-01..04 xong)
 20.  TEST-02    Verify API-02 unauthorized test pass
 21.  BUG-04     Remove duplicate using MediatR trong LoggingBehavior.cs
 22.  EH-05      SendSmsHandler (khi có SMS provider)
 23.  FEAT-01    MetricsService thực tế với Prometheus
 24.  FEAT-02    Email recipient động từ task settings
```
