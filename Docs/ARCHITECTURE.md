# Task Scheduler System — Architecture & Project Reference

> The single reference for the whole project: architecture, domain model, task lifecycle,
> full API, execution & retry mechanics, security, configuration, testing, CI/CD,
> deployment and operations. Read the [README](../README.md) for a quick start; read this
> for *how everything works and why*.

---

## Table of Contents

1. [Overview](#1-overview)
2. [Technology Stack](#2-technology-stack)
3. [Architecture](#3-architecture)
4. [Task Lifecycle](#4-task-lifecycle)
5. [Domain Model](#5-domain-model)
6. [API Reference](#6-api-reference)
7. [CQRS Application Layer](#7-cqrs-application-layer)
8. [Execution Engine & Retries](#8-execution-engine--retries)
9. [Security](#9-security)
10. [CI/CD & Deployment](#10-cicd--deployment)
11. [Testing the Full Lifecycle (End-to-End Walkthrough)](#11-testing-the-full-lifecycle-end-to-end-walkthrough)
12. [Automated Testing](#12-automated-testing)
13. [Database Schema](#13-database-schema)
14. [Known Issues & Design Notes](#14-known-issues--design-notes)

---

## 1. Overview

The Task Scheduler System is an enterprise-style backend platform for **scheduling,
executing and monitoring background tasks**. Each task wraps an arbitrary **OS shell
command** and a **cron expression**; the system runs the command on schedule, records a
per-run execution log (duration, exit code, output, errors), retries failures with
exponential backoff, and emails an admin when a task exhausts its retries.

Design goals:

- **Clean Architecture** — strict dependency direction, zero-dependency domain
- **CQRS + MediatR** — every use case is an isolated command/query handler
- **Domain events** — side effects (logging, metrics, email) react to state changes, decoupled from the state change itself
- **Hangfire + PostgreSQL** — durable scheduling; jobs survive process restarts
- **12-factor configuration** — secrets via environment variables, no committed credentials
- **Docker + GitHub Actions** — reproducible image, automated CI/CD, one-button deployment

The system mirrors the concept of the Windows Task Scheduler, but as a service:
JWT-secured REST API, web dashboard UI, and infrastructure-as-code deployment.

---

## 2. Technology Stack

| Area | Technology | Notes |
|---|---|---|
| Language / runtime | **C# / .NET 10** (`net10.0`) | All projects target `net10.0` |
| Web framework | **ASP.NET Core** (minimal hosting, `Program.cs`) | API + static files in one process |
| Architecture | **Clean Architecture**, **CQRS** (MediatR 14.1), Repository + Unit of Work | |
| Validation | **FluentValidation 12.1** | Runs as a MediatR pipeline behavior |
| Mapping | **AutoMapper 12.0** | Two mapping profiles |
| Database | **PostgreSQL 18** + **EF Core 9** (Npgsql 9) | One database for app data *and* Hangfire |
| Scheduling | **Hangfire 1.8.23** (+ `Hangfire.PostgreSql` 1.21.1) | Recurring jobs, in-process server, Postgres job store |
| Cron parsing | **Cronos 0.13** (domain layer) + Hangfire's cron (scheduler) | Domain validates at creation/update time |
| Auth | **JWT Bearer 10** (HMAC-SHA256) + PBKDF2 password hashing | `System.IdentityModel.Tokens.Jwt` 8 |
| Email | `System.Net.Mail.SmtpClient` | Failure alerts only |
| Logging | **Serilog 10** | Console + rolling daily file (`logs/log-YYYYMMDD.txt`) |
| Tracing | **OpenTelemetry 1.16/1.15** | ASP.NET Core + HttpClient instrumentation, console exporter |
| API docs | **Swashbuckle 10** (annotations) | Development environment only |
| Frontend | Vanilla HTML/CSS/JS (`wwwroot`) | No framework, token in `localStorage` |
| Packaging | **Docker** multi-stage (`sdk:10.0` → `aspnet:10.0`), **docker compose** | Port 8080 |
| CI/CD | **GitHub Actions** → **GitHub Container Registry** → **Railway** | See [§10](#10-cicd--deployment) |
| Testing | **xUnit 2.9**, **FluentAssertions 8**, **Moq 4.20**, EF Core InMemory/SQLite, Hangfire.InMemory, `WebApplicationFactory` | See [§12](#12-automated-testing) |

---

## 3. Architecture

### 3.1 Layers & dependency direction

```text
┌────────────────────────┐
│        Api             │  Controllers, middleware, Swagger, Hangfire dashboard,
│  (ASP.NET Core Web)    │  JWT auth, rate limiting, Serilog, OpenTelemetry, UI hosting
└───────────┬────────────┘
            │
┌───────────▼────────────┐      ┌────────────────────────────────┐
│      Application       │◄─────│           Infrastructure       │
│  CQRS (MediatR),       │      │  EF Core + PostgreSQL,         │
│  FluentValidation,     │      │  Hangfire scheduler + job      │
│  AutoMapper, pipeline  │      │  store, command execution,     │
│  behaviors, domain-    │      │  JWT/password, SMTP,           │
│  event handlers, DTOs, │      │  repositories, Unit of Work    │
│  service interfaces    │      └────────────────────────────────┘
└───────────┬────────────┘
            │
┌───────────▼────────────┐
│        Domain          │  ScheduledTask, TaskExecutionLog, User,
│  (no dependencies)     │  enums, CronExpression VO, domain events
└────────────────────────┘
```

| Layer | Project | Depends on | Owns |
|---|---|---|---|
| **Domain** | `TaskScheduler.Domain` | — (only Cronos) | Entities, enums, value object, domain events, state-transition rules |
| **Application** | `TaskScheduler.Application` | Domain | Use cases (CQRS), behaviors, DTOs, mapping profiles, event handlers, **all service interfaces** |
| **Infrastructure** | `TaskScheduler.Infrastructure` | Application, Domain | EF Core persistence, Hangfire, JWT, SMTP, repositories, `IUnitOfWork` impl |
| **Api** | `TaskScheduler.Api` | Application, Infrastructure | HTTP surface: controllers, middleware, DI composition root, static UI |

Dependency rule: **outer layers may depend on inner layers, never the reverse.**
The Application layer declares interfaces (`ISchedulerService`, `ITaskExecutionService`,
`IEmailService`, …) that Infrastructure implements — the Application layer never
references Hangfire, EF Core or SMTP.

### 3.2 Project structure

```text
src/TaskScheduler.Domain/
├── Common/            BaseEntity, DomainEvent, IDomainEvent
├── Entities/          ScheduledTask, TaskExecutionLog, User
├── Enums/             ScheduledTaskStatus, TaskExecutionStatus
├── Events/            TaskCreatedEvent, TaskPausedEvent, TaskFailedEvent, TaskCompletedEvent
└── ValueObjects/      CronExpression

src/TaskScheduler.Application/
├── Auth/              Login/Register commands, validators, AuthResponse
├── Common/
│   ├── Behaviors/     LoggingBehavior, ValidationBehavior
│   ├── Mappings/      TaskMappingProfile, ExecutionLogMappingProfile
│   ├── Mediator/      DomainEventNotification<T>
│   ├── Models/        ApiResponse<T>, PagedResult<T>, CommandExecutionResult, EmailMessage, SmsMessage
│   └── Telemetry/     TelemetryConfig (ActivitySource "TaskScheduler")
├── EventHandlers/
│   ├── Logging/       TaskCreated/TaskCompleted/TaskFailed log handlers
│   ├── Metrics/       TaskCompleted/TaskFailed metrics handlers
│   └── Notifications/ SendEmailHandler (admin alert on TaskFailedEvent)
├── Interfaces/        ITaskRepository, ITaskExecutionLogRepository, IUserRepository,
│                      IUnitOfWork, ISchedulerService, ITaskExecutionService,
│                      ITokenService, IEmailService, ISmsService, IMetricsService,
│                      IExecutionLogService, ISensitiveRequest
└── Tasks/
    ├── Commands/      CreateTask, UpdateTask, DeleteTask, ActiveTask, PauseTask, ResumeTask, TriggerTask
    └── Queries/       GetTasks, GetTaskById, GetTasksPaged, GetTasksDashboard,
                       GetTaskExecutionLogs, GetDetailsTaskExecutionLog

src/TaskScheduler.Infrastructure/
├── Persistence/       ApplicationDbContext (+ SaveChangesAsync event dispatch), entity configurations, Migrations/
├── Repositories/      TaskRepository, TaskExecutionLogRepository, UserRepository
├── Scheduling/        HangfireSchedulerService, TaskExecutionService, TaskJob
└── Services/          TokenService, SmtpEmailService, NoOpSmsService, MetricsService, ExecutionLogService

src/TaskScheduler.Api/
├── Controllers/       BaseApiController, AuthController, TasksController, ExecutionLogsController, DashboardController
├── Middleware/        CorrelationIdMiddleware, ExceptionHandlingMiddleware
├── Extensions/        Authentication, Hangfire, RateLimit, Cors, Swagger, Telemetry
├── Authorization/     HangfireAuthorizationFilter (loopback-only)
└── wwwroot/           index.html (dashboard), tasks.html, logs.html, login.html, js/api.js, css/app.css

Tests/
├── TaskScheduler.Domain.Tests/          (in .sln)
├── TaskScheduler.Api.Tests/             (in .sln)
├── TaskScheduler.Application.Tests/     (on disk, NOT registered in .sln)
└── TaskScheduler.Infrastructure.Tests/  (on disk, NOT registered in .sln)
```

### 3.3 Startup pipeline (what `Program.cs` does, in order)

**Service registration:**
1. `WebApplication.CreateBuilder(args)`
2. **Serilog** — configured from the `Serilog` appsettings section
3. MVC controllers, endpoint API explorer, `IHttpContextAccessor`
4. `AddInfrastructure(configuration, environment)` — EF Core (Npgsql), Hangfire (Postgres storage + in-process server), `ISchedulerService`, and all scoped services (repositories, `ITaskExecutionService`, `ITokenService`, `IEmailService`, `ISmsService`, `IMetricsService`, `IExecutionLogService`, `IUnitOfWork` → the `ApplicationDbContext` itself). **Skipped in the `Testing` environment** (tests wire their own).
5. `AddApplication()` — MediatR (assembly scan), FluentValidation, AutoMapper, pipeline behaviors (order matters: `LoggingBehavior` registered first = outermost, then `ValidationBehavior`)
6. Swagger (annotations, bearer security scheme)
7. JWT authentication (see [§9](#9-security))
8. OpenTelemetry tracing
9. CORS (`AllowAll`)
10. Rate limiting (`trigger-policy`: fixed window, 10 permits/minute)
11. Health checks (+ Npgsql probe when a connection string is present)

**Middleware pipeline (request order):**
1. `UseHangfireDashboard("/hangfire")` — mapped **first**, so it bypasses everything below (and applies its own IP filter)
2. `CorrelationIdMiddleware` — reads/creates `X-Correlation-Id`, stores it in `HttpContext.Items`, echoes it in the response, pushes it into Serilog `LogContext`
3. `ExceptionHandlingMiddleware` — maps exceptions to JSON `ApiResponse` (see [§6.1](#61-conventions))
4. Swagger UI — **only in Development**
5. `UseDefaultFiles` + `UseStaticFiles` — serves the `wwwroot` web UI at `/`
6. HTTPS redirection → CORS → Serilog request logging → routing → authentication → authorization → rate limiter
7. `MapControllers()`, `MapHealthChecks("/health")`

**Post-startup:** unless the environment is `Testing`, a scope is created and
`ApplicationDbContext.Database.MigrateAsync()` runs — **EF Core migrations apply
automatically at every startup** (idempotent, so it is safe on each container boot).

### 3.4 A CQRS request, step by step

Example: `POST /api/v1/tasks`

```text
1. Middleware: correlation id attached, exception guard installed
2. JWT: Authorization: Bearer <token> validated (all /tasks endpoints are [Authorize])
3. TasksController → _mediator.Send(new CreateTaskCommand(...))
4. LoggingBehavior:
     - payload serialized to JSON (or "[REDACTED]" if request implements ISensitiveRequest)
     - OTel activity "CreateTaskCommand" started (tags: request.name, request.body, correlation.id)
     - stopwatch started; slow-request warning if the handler takes > 1000 ms
     - on exception: error tags + log + rethrow
5. ValidationBehavior:
     - runs all FluentValidation validators for the command concurrently
     - any failure → FluentValidation.ValidationException → (mapped to 400 by middleware)
6. CreateTaskHandler:
     - new ScheduledTask(...)        → domain ctor validates cron, raises TaskCreatedEvent
     - task.UpdateNextRunTime()      → Cronos computes NextRunAt
     - ITaskRepository.AddAsync + IUnitOfWork.SaveChangesAsync
7. ApplicationDbContext.SaveChangesAsync:
     - writes to PostgreSQL
     - dispatches TaskCreatedEvent as DomainEventNotification<TaskCreatedEvent> via MediatR
8. TaskCreatedLogHandler logs "Task created: {id} ({name})"
9. Controller wraps the result: 200 OK { "code": 0, "message": "Task Created successfully", "data": "<guid>" }
```

### 3.5 The web UI

A dependency-free single-page-per-view app in `wwwroot`, served by the API itself:

| Page | Route | What it does |
|---|---|---|
| Login / Register | `/login.html` | Tabbed form; stores JWT + username in `localStorage`; redirects to `/` when a token exists |
| Dashboard | `/` (`index.html`) | Six stat cards from `GET /api/v1/dashboard` + recent-tasks table (paged, page size 8), manual refresh |
| Tasks | `/tasks.html` | Paged list (server-side status filter + client search), create/edit modals with a cron cheat-sheet, detail modal with **status-dependent action buttons** (`pending`→Activate; `active`→Trigger Now, Pause; `paused`→Resume; `failed`→Re-activate, Trigger Now), per-task logs table, delete confirmation, deep-link `?id={guid}` |
| Execution logs | `/logs.html` | Task dropdown + per-run table (start/finish/duration/status/error), deep-link `?taskId={guid}` |

`js/api.js` is the whole client: `apiFetch()` injects the bearer token, treats
**401 as a global "go to login"** (clears `localStorage`), unwraps the
`{ code, message, data }` envelope, and exposes `Auth.*`, `Tasks.*`, `Dashboard.*`
namespaces plus toast/badge helpers.

### 3.6 Observability

| Concern | Mechanism |
|---|---|
| Structured logging | Serilog: console + file `logs/log-YYYYMMDD.txt` (daily rolling). Levels: `Information` default, `Warning` for `Microsoft`/`System` |
| Correlation | `X-Correlation-Id` request/response header; every log line and every OTel span carries it |
| Tracing | OpenTelemetry: service name `TaskScheduler`; ASP.NET Core + HttpClient auto-instrumentation; **per-MediatR-request activity** created in `LoggingBehavior` (span per command/query); **slow-request warning** above 1 s; spans exported to **console** |
| Health | `GET /health` — overall + Npgsql liveness probe (registered only when a connection string exists) |
| Scheduling visibility | Hangfire dashboard at `/hangfire` (recurring jobs, background/recurring queues, history) |
| Business metrics | `IMetricsService.IncrementCompletedTasksAsync/IncrementFailedTasksAsync` are invoked via domain-event handlers but are **currently no-ops** (no metrics backend wired — see [§14](#14-known-issues--design-notes)) |

### 3.7 Configuration reference

`appsettings.json` defines the structure; **secrets are always injected from the
environment** (docker compose `.env`, GitHub Actions env, or a gitignored
`appsettings.Development.json` for local `dotnet run`).

| Key | Default | Purpose |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | *(empty)* | PostgreSQL connection string — used by **EF Core and Hangfire** (same database) |
| `Jwt:Secret` | *(empty)* | HMAC-SHA256 signing key; **must be ≥ 32 chars** |
| `Jwt:Issuer` | `TaskSchedulerAPI` | Validated on both issue and verify |
| `Jwt:Audience` | `TaskSchedulerClient` | Validated on both issue and verify |
| `Jwt:ExpireHours` | `72` | Token lifetime (3 days) |
| `TaskExecution:TimeoutSeconds` | `300` | Per-run command timeout; process tree is killed on expiry |
| `Smtp:Host` / `Smtp:Port` / `Smtp:User` / `Smtp:Password` | `smtp.gmail.com` / `587` / — / — | Failure-alert email (user is also the sender address); STARTTLS |
| `Notifications:AdminEmail` | *(empty)* | Recipient of failure alerts; empty ⇒ no email sent |
| `Serilog:*` | console + file sinks | Logging configuration |
| `ASPNETCORE_ENVIRONMENT` | — | `Development` (Swagger + UI tweaks), `Production` (compose default), `Testing` (CI) |

Docker compose exposes these as `DB_PASSWORD`, `JWT_SECRET`, `SMTP_HOST`,
`SMTP_PORT`, `SMTP_USER`, `SMTP_PASSWORD`, `NOTIFICATIONS_ADMIN_EMAIL` (see
`.env.example`).

---

## 4. Task Lifecycle

### 4.1 States

`ScheduledTaskStatus` (domain enum, stored as integer in `ScheduledTasks.Status`):

| Value | State | Meaning |
|---|---|---|
| 0 | `Pending` | Created. **Not** registered with Hangfire — it will never run until activated |
| 1 | `Active` | Recurring job registered; waits for its cron slots |
| 2 | `Running` | An execution is in progress (transient) |
| 3 | `Paused` | User-paused; recurring job removed; row kept |
| 4 | `Failed` | Last run failed **and** the retry budget is exhausted |
| 5 | `Completed` | Declared "execution finished successfully" — see [§14](#14-known-issues--design-notes) (never set on the current execution path) |

`TaskExecutionStatus` (per run, stored on `TaskExecutionLogs.Status`):
`Running = 0`, `Success = 1`, `Failed = 2`.

### 4.2 State transition diagram

```text
                     POST /api/v1/tasks
                            │
                            ▼
             ┌────────┐  POST /{id}/activate  ┌────────┐
             │Pending │──────────────────────►│ Active │
             └────────┘                       └───┬────┘
                                                 │
          ┌──────────────────────────────────────┤
          │ POST /{id}/pause (only from Active)   │ Hangfire cron fires
          ▼                                       ▼  (or POST /{id}/trigger from Active/Failed)
     ┌────────┐                        ┌────────┐   1. log → Running
     │ Paused │                        │Running│   2. shell command executes
     └───┬────┘                        └───┬────┘
         │ POST /{id}/resume (only from    │
         └────────────────► Active ◄───────┤
                         (re-registers)    │
                    success ───────────────┤────────────── failure
                    back to Active:        │            │
                    ResetRetryCount,       │            ├─ RetryCount ≤ MaxRetries:
                    UpdateNextRunTime,     │            │   back to Active + delayed
                                           │            │   Hangfire job (60s/120s/240s/300s cap)
                                           │            │
                                           │            └─ RetryCount > MaxRetries:
                                           │                 → Failed (TaskFailedEvent →
                                           │                   error log + admin email)
                                           │
      Failed tasks: trigger is still allowed (POST /{id}/trigger)
```

### 4.3 Transition rules (as enforced by the code)

| Transition | Triggered by | Guard (where enforced) | Hangfire effect |
|---|---|---|---|
| — → `Pending` | `POST /tasks` (ctor) | cron validated by `CronExpression.Create` | **none** — job not registered |
| `Pending` → `Active` | `POST /tasks/{id}/activate` | handler: "Only pending tasks can be activated." | `AddOrUpdate` recurring job (id = task GUID, cron = task cron) |
| `Active` → `Paused` | `POST /tasks/{id}/pause` | **entity**: "Only active task can be paused." | recurring job removed |
| `Paused` → `Active` | `POST /tasks/{id}/resume` | handler: "Only paused tasks can be resumed." | recurring job re-registered |
| `Active`/`Failed` → (run) | `POST /tasks/{id}/trigger` (rate-limited) | handler: "Only Active or Failed tasks can be triggered manually." | one **background** (non-recurring) job enqueued |
| *(any)* → `Running` | Hangfire fires (cron, trigger, or delayed retry) | **none** in `TaskExecutionService.ExecuteTask` | — |
| `Running` → `Active` | run succeeded | — | `NextRunAt` recomputed, `RetryCount` reset |
| `Running` → `Active` + delayed job | run failed, retries left | `RetryCount <= MaxRetries` (checked **after** increment) | one **delayed** job with backoff |
| `Running` → `Failed` | run failed, retries exhausted | `RetryCount > MaxRetries` | **recurring job is NOT removed** (see [§14](#14-known-issues--design-notes)) |

Rules of thumb:
- A task is only ever *scheduled* by the explicit lifecycle actions above — except that
  `PUT /tasks/{id}` (update) calls `RescheduleTaskAsync` **unconditionally**, which also
  registers a recurring job for `Pending` tasks (see [§14](#14-known-issues--design-notes)).
- Soft-deleted tasks (`IsDeleted = true`) are invisible to the repositories, and a fired
  job for a deleted task simply logs "Task not found" and returns.
- Domain events raised during the lifecycle: `TaskCreatedEvent` (ctor),
  `TaskPausedEvent` (`Pause()`), `TaskFailedEvent` (`MarkAsFailed`),
  `TaskCompletedEvent` (`MarkAsCompleted` — currently unused by the execution engine).

### 4.4 Execution & retry mechanics

On each run (`TaskExecutionService.ExecuteTask`):

1. A `TaskExecutionLog` is created (`Running`, `StartedAt = now`).
2. The task is set to `Running` (`LastRunAt = now`) and everything is persisted.
3. The command runs (see [§8](#8-execution-engine--retries)); stdout/stderr, exit code
   and wall-clock duration are captured.
4. **Success path** (`exit code == 0`): log → `Success` (+`FinishedAt`, `DurationMs`),
   task → `Active`, `NextRunAt` recomputed via Cronos, `RetryCount` reset to 0.
5. **Failure path** (non-zero exit, timeout, or exception): log → `Failed`
   (`ErrorMessage` = stderr, or "Command exited with code N" when stderr is empty),
   `RetryCount++`, then:
   - `RetryCount <= MaxRetries` → task stays `Active` and a **delayed** Hangfire job
     re-runs it after `min(300, 2^RetryCount × 30)` seconds;
   - `RetryCount > MaxRetries` → task → `Failed`, which raises `TaskFailedEvent`
     → error log + **admin email** (if `Notifications:AdminEmail` is set).

**Backoff table** (delay computed with the *already-incremented* `RetryCount`):

| Failure # | RetryCount after | Delay until next attempt |
|---|---|---|
| 1 | 1 | `2^1 × 30` = **60 s** |
| 2 | 2 | `2^2 × 30` = **120 s** |
| 3 | 3 | `2^3 × 30` = **240 s** |
| 4+ | 4+ | capped at **300 s** |

With `maxRetries = N` a task gets **N + 1 total attempts** (1 initial + N retries).
`maxRetries = 0` means the first failure puts the task into `Failed` immediately.

### 4.5 Domain events & side effects

Events are collected on entities, **flushed after `SaveChangesAsync`** by
`ApplicationDbContext.SaveChangesAsync` (which clears the entity's event list and
publishes each event as `DomainEventNotification<T>` through MediatR), then handled
in-process:

| Domain event | Raised by | Handlers (Application layer) | Side effects |
|---|---|---|---|
| `TaskCreatedEvent(TaskId, Name)` | `ScheduledTask` constructor | `TaskCreatedLogHandler` | Info log |
| `TaskPausedEvent(TaskId)` | `ScheduledTask.Pause()` | *(none — raised but unhandled)* | — |
| `TaskFailedEvent(TaskId, Reason)` | `ScheduledTask.MarkAsFailed(reason)` | `TaskFailedLogHandler`, `TaskFailedMetricsHandler`, `SendEmailHandler` | Error log; metrics increment (no-op impl); **email to `Notifications:AdminEmail`** ("Task {id} failed") |
| `TaskCompletedEvent(TaskId)` | `ScheduledTask.MarkAsCompleted()` | `TaskCompletedLogHandler`, `TaskCompletedMetricsHandler` | Info log; metrics increment (no-op impl). *Not raised by the current execution engine* |

The event flow is the seam for future side effects (push notifications, webhooks,
Kafka outbox, …) without touching the state-transition code.

---

## 5. Domain Model

### 5.1 `ScheduledTask` (`BaseEntity`)

| Property | Type | Notes |
|---|---|---|
| `Id` | `Guid` | Generated in the domain ctor |
| `Name` | `string` | Required, ≤ 100 chars (validator + DB constraint) |
| `Description` | `string` | ≤ 256 chars |
| `CronExpression` | `CronExpression` (VO) | Validated with Cronos at creation/update |
| `Command` | `string` | The shell command to execute, ≤ 256 chars |
| `Status` | `ScheduledTaskStatus` | See [§4.1](#41-states) |
| `LastRunAt` / `NextRunAt` | `DateTime?` | Set by the execution engine |
| `RetryCount` / `MaxRetries` | `int` | `MaxRetries` validated 0–10 by the API validator |
| `IsDeleted` | `bool` | Soft-delete flag; repositories filter it out |
| `CreatedAt` / `UpdatedAt` | `DateTime` | UTC |
| `ExecutionLogs` | `ICollection<TaskExecutionLog>` | Navigation (one-to-many) |

Behavior methods: `MarkAsActive()`, `MarkAsRunning()`, `Pause()` (guarded: only from
`Active`), `MarkAsFailed(reason)` (raises `TaskFailedEvent`), `MarkAsCompleted()`
(raises `TaskCompletedEvent`), `UpdateNextRunTime()`, `SoftDelete()`,
`Update(...)` (validates non-empty name and non-negative maxRetries),
`IncreaseRetryCount()` / `ResetRetryCount()`.

### 5.2 `TaskExecutionLog` (standalone POCO — no domain events)

| Property | Type | Notes |
|---|---|---|
| `Id` | `Guid` | |
| `TaskId` | `Guid` | FK → `ScheduledTasks` (cascade delete) |
| `ScheduledTask` | navigation | |
| `StartedAt` | `DateTime` | |
| `FinishedAt` | `DateTime?` | Null while running |
| `Status` | `TaskExecutionStatus` | `Running → Success/Failed` (guarded) |
| `ErrorMessage` | `string?` | Carries stderr (or the failure reason), ≤ 1000 chars |
| `DurationMs` | `long?` | Wall-clock duration of the process |
| `ExitCode` | `int?` | Added by the 3rd migration |

Methods: `MarkAsSuccess()` / `MarkAsFailed(message)` (both guarded: only from
`Running`), `Retry()` (resets the log to a fresh `Running` state),
`SetExecutionDetails(errorMessage, durationMs, exitCode)`.

### 5.3 `User` (`BaseEntity`)

`Id`, `Username` (≤ 50), `Email`, `PasswordHash` (PBKDF2, base64 `salt || hash`),
`CreatedAt`, `UpdatedAt`, `IsActive`. Methods: `Activate()`, `Deactivate()`,
`ChangePassword(hash)`. No domain events.

### 5.4 `CronExpression` value object

- Factory `CronExpression.Create(string)` — empty → `ArgumentException`; syntactically
  invalid (checked with **Cronos**) → `ArgumentException("Invalid cron expression: ...")`.
- `GetNextOccurrence(from)` — next UTC occurrence (used for `NextRunAt`).
- Value equality by the raw string.

### 5.5 Base types

- `BaseEntity` — holds a private `List<IDomainEvent>`; `AddDomainEvent` /
  `ClearDomainEvents` (flushed by the Unit of Work, see [§4.5](#45-domain-events--side-effects)).
- `DomainEvent` — abstract record with `OccurredOn = DateTime.UtcNow`.
- No domain services; all cross-entity abstractions live in Application `Interfaces`.

---

## 6. API Reference

Base URL: `/api/v1`. All task/dashboard endpoints require a JWT
(`Authorization: Bearer <token>`).

### 6.1 Conventions

**Response envelope** (every endpoint):

```json
{ "code": 0, "message": "Task Created successfully", "data": "3f7c…guid" }
```

- `code` `0` = success, `1` = generic failure.
- Error mapping in `ExceptionHandlingMiddleware`:

| Exception | HTTP | Body |
|---|---|---|
| `ArgumentException` | 400 | exception message |
| `InvalidOperationException` | 400 | exception message (all lifecycle guards surface here) |
| `KeyNotFoundException` | 404 | exception message |
| `UnauthorizedAccessException` | 401 | exception message |
| anything else | 500 | generic "An internal server error occurred" |

- Invalid JWT / missing token → standard **401** (before reaching controllers).
- Rate limit exceeded on trigger → **429**.
- Pagination model `PagedResult<T>`: `{ items, totalCount, page, pageSize, totalPages, hasNextPage, hasPreviousPage }`.

### 6.2 Authentication (`/api/v1/auth` — public)

**`POST /auth/register`**

```json
{ "username": "devuser", "email": "dev@test.com", "password": "Test@1234", "confirmPassword": "Test@1234" }
```

Validation: username ≤ 50; valid email; password ≥ 8 chars; passwords must match.
Errors: duplicate username/email → 400 "Username already exists." / "Email already exists."
Response `data`: the new user `Guid`.

**`POST /auth/login`**

```json
{ "username": "devuser", "password": "Test@1234" }
```

Response `data`:

```json
{ "token": "eyJ0eXAi…", "username": "devuser", "email": "dev@test.com" }
```

Errors: bad credentials → 401 "Invalid username or password."; disabled account →
400 "Account is disabled." The login command implements `ISensitiveRequest`, so its
payload is logged as `[REDACTED]` (passwords never hit the log).

### 6.3 Tasks (`/api/v1/tasks` — `[Authorize]`)

| Method & route | Request | `data` |
|---|---|---|
| `POST /tasks` | body below | new task `Guid` |
| `GET /tasks` | — | `[{ id, name, status }]` |
| `GET /tasks/paged?page=1&pageSize=10&status=Active` | query | `PagedResult<TaskDto>` (ordered by `CreatedAt` desc) |
| `GET /tasks/{id}` | — | `TaskDto` |
| `PUT /tasks/{id}` | partial body (any subset) | `{}` (`Unit`) |
| `DELETE /tasks/{id}` | — | `{}` — soft delete + unschedule |
| `POST /tasks/{id}/activate` | — | `{}` — `Pending` → `Active`, registers recurring job |
| `POST /tasks/{id}/pause` | — | `{}` — `Active` → `Paused`, removes recurring job |
| `POST /tasks/{id}/resume` | — | `{}` — `Paused` → `Active`, re-registers job |
| `POST /tasks/{id}/trigger` | — | `{}` — immediate background run (429 over 10/min) |

**Create body** (all required):

```json
{
  "name": "Nightly backup",
  "description": "Backs up the reports folder",
  "cronExpression": "0 2 * * *",
  "command": "robocopy D:\\data D:\\backup /MIR",
  "maxRetries": 3
}
```

Validation: `name` non-empty ≤ 100; `command` non-empty; `cronExpression` non-empty
(and must parse — the domain VO re-validates it); `maxRetries` 0–10.

**Update body** (all optional, `null` = keep current value):

```json
{ "name": null, "description": "new desc", "cronExpression": "0 3 * * *", "command": null, "maxRetries": 5 }
```

`TaskDto` (detail & paged responses): `{ id, name, description, status, cronExpression }`.

### 6.4 Execution logs (`/api/v1/tasks` — `[Authorize]`)

| Method & route | `data` |
|---|---|
| `GET /tasks/{id}/logs` | `[{ id, startedAt, finishedAt, status, errorMessage }]` |
| `GET /tasks/{id}/log/{logId}` | `{ id, taskId, taskName, startedAt, finishedAt, status, errorMessage, durationMs }` (404 if the log doesn't belong to the task) |

### 6.5 Dashboard & health

| Route | Auth | Description |
|---|---|---|
| `GET /api/v1/dashboard` | yes | `{ totalTasks, activeTasks, pausedTasks, deletedTasks, runningTasks, failedExecutions, successExecutions }` — computed in memory from the full task + log collections |
| `GET /health` | no | Health checks (`Healthy` + `npgsql` result when a connection string is set) |
| `GET /hangfire` | loopback IP only | Hangfire dashboard (jobs, queues, history) |

---

## 7. CQRS Application Layer

### 7.1 Commands

| Command | Returns | Guard / behavior |
|---|---|---|
| `CreateTaskCommand(Name, Description, CronExpression, Command, MaxRetries)` | `Guid` | Builds `ScheduledTask` (status `Pending`), computes `NextRunAt`, saves. **Does not register a Hangfire job** |
| `UpdateTaskCommand(Id, Name?, Description?, CronExpression?, Command?, MaxRetries?)` | `Unit` | 400 if missing/deleted; applies non-null fields; **always** calls `RescheduleTaskAsync` |
| `DeleteTaskCommand(Id)` | `Unit` | Soft delete + `UnscheduleTaskAsync` |
| `ActiveTaskCommand(Id)` | `Unit` | "Only pending tasks can be activated." → `ScheduleTaskAsync` |
| `PauseTaskCommand(Id)` | `Unit` | Entity guard "Only active task can be paused." → `UnscheduleTaskAsync` |
| `ResumeTaskCommand(Id)` | `Unit` | "Only paused tasks can be resumed." → `RescheduleTaskAsync` |
| `TriggerTaskCommand(Id)` | `Unit` | "Only Active or Failed tasks can be triggered manually." → `ITaskExecutionService.TriggerNow` (background job) |
| `LoginCommand(Username, Password)` | `AuthResponse` | `ISensitiveRequest`; 401 on bad credentials, 400 on disabled account |
| `RegisterCommand(Username, Email, Password, ConfirmPassword)` | `Guid` | Duplicate checks; PBKDF2 hash; new user is active |

### 7.2 Queries

| Query | Returns | Notes |
|---|---|---|
| `GetTasksQuery` | `List<TaskSummaryDto>` | Non-deleted tasks |
| `GetTaskByIdQuery(Id)` | `TaskDto` | 404 via `KeyNotFoundException` |
| `GetTasksPagedQuery(Page, PageSize, Status?)` | `PagedResult<TaskDto>` | `CreatedAt` desc, optional status filter |
| `GetTasksDashboardQuery` | `TasksDashboardDto` | In-memory aggregation |
| `GetTaskExecutionLogsQuery(TaskId)` | `List<ExecutionLogDto>` | Per-task logs |
| `GetDetailsTaskExecutionLogQuery(TaskId, LogId)` | `TaskExecutionLogDetailsDto` | 404 on mismatch |

### 7.3 Pipeline behaviors (order = registration order)

1. **`LoggingBehavior<TReq,TRes>`** (outermost) — correlation id + payload logging
   (`[REDACTED]` for `ISensitiveRequest`), OTel activity per request, timing,
   slow-request warning (> 1 s), error logging + rethrow.
2. **`ValidationBehavior<TReq,TRes>`** — runs all FluentValidation validators for the
   request concurrently; throws `ValidationException` on failure.

Validators exist for: `CreateTask`, `UpdateTask`, `Login`, `Register`. The lifecycle
commands carry no validators — their guards are in handler/entity code.

### 7.4 Cross-cutting models

| Model | Shape |
|---|---|
| `ApiResponse<T>` | `{ code, message, data }` + `SuccessResponse`/`FailureResponse` helpers |
| `PagedResult<T>` | `{ items, totalCount, page, pageSize, totalPages, hasNextPage, hasPreviousPage }` |
| `CommandExecutionResult` | `{ success, exitCode, standardOutput, standardError, duration }` |
| `EmailMessage` / `SmsMessage` | `{ to, subject, body }` / `{ phoneNumber, message }` |
| `DomainEventNotification<T>` | MediatR notification wrapper for domain events |

---

## 8. Execution Engine & Retries

### 8.1 Running a command (`TaskExecutionService.ExecuteCommand`)

- **Windows**: `cmd.exe /c <command>` — **Linux**: `/bin/sh -c "<command>"`
  (detected via `RuntimeInformation`).
- Process options: stdout & stderr redirected and read **concurrently** (avoids pipe
  deadlock), no window, no shell execute.
- **Timeout**: `TaskExecution:TimeoutSeconds` (default 300 s). On expiry the whole
  process tree is killed (`process.Kill(true)`) and a `TimeoutException` is thrown —
  which flows into the normal failure path (log `Failed`, retry, etc.).
- **Success** ⇔ **exit code 0** — regardless of any output content.
- Duration is measured from before process start to exit (wall clock).

### 8.2 Hangfire integration

| Piece | Detail |
|---|---|
| Storage | **PostgreSQL** — the same database as app data (`Hangfire.PostgreSql`), so the job catalog survives restarts |
| Server | In-process (`AddHangfireServer`), single default queue, no custom queues |
| Recurring jobs | `IRecurringJobManager.AddOrUpdate<TaskJob>(id, job, cron)` with **job id = task GUID as string**; cron string parsed by Hangfire |
| Background jobs | `IBackgroundJobClient.Enqueue` (manual trigger) and `Schedule` (delayed retries) |
| `TaskJob` | Thin class: `Execute(Guid taskId)` → `ITaskExecutionService.ExecuteTask(taskId)` |
| Dashboard | `/hangfire`, protected by `HangfireAuthorizationFilter` (loopback IPs only) |

**Which operation touches Hangfire:** create → *none*; activate → add recurring;
pause/delete → remove; resume/update → add-or-update; trigger → background job;
failed run with retries left → delayed job. Because the job store is PostgreSQL,
**recurring schedules survive an app restart automatically** — no re-registration code
is needed at startup (only database migrations run at boot).

### 8.3 Retry algorithm

```text
on failure:
    RetryCount += 1
    if RetryCount <= MaxRetries:
        task → Active
        Schedule delay = min(300, 2^RetryCount * 30) seconds   # 60s, 120s, 240s, 300s, 300s...
        Hangfire.Schedule(TaskExecutionService.ExecuteTask, delay)
    else:
        task → Failed          # raises TaskFailedEvent → error log + admin email
```

Properties worth knowing:
- Retries are **per-task, cumulative across runs** — a success resets `RetryCount` to 0.
- A task that is `Failed` keeps its recurring job registered (see [§14](#14-known-issues--design-notes)).
- Delayed retry jobs are independent of the recurring job — pausing a task does not
  cancel an already-scheduled retry (see [§14](#14-known-issues--design-notes)).

---

## 9. Security

| Control | Implementation |
|---|---|
| Authentication | JWT Bearer; every task/dashboard/log endpoint is `[Authorize]`; only `/auth/*`, `/health`, the UI and `/hangfire` are open |
| Token | HMAC-SHA256 with `Jwt:Secret` (≥ 32 chars), issuer `TaskSchedulerAPI`, audience `TaskSchedulerClient`, claims: `NameIdentifier` (user id), `Name` (username), `Email`; **72 h** expiry (`Jwt:ExpireHours`) |
| Passwords | PBKDF2 (`Rfc2898DeriveBytes`), 10 000 iterations, SHA-256, 16-byte random salt, 32-byte hash, stored as `base64(salt ‖ hash)` |
| Request payload protection | `LoginCommand` implements `ISensitiveRequest` → payload logged as `[REDACTED]` |
| Rate limiting | ASP.NET Core fixed-window limiter: `trigger-policy` = **10 requests/minute** on `POST /tasks/{id}/trigger` only; 429 when exceeded |
| Hangfire dashboard | `IDashboardAuthorizationFilter` allows **127.0.0.1 / ::1 only** (no credential, so remote access is impossible even with a reverse proxy that rewrites the source IP) |
| CORS | Single `AllowAll` policy (any origin/method/header) — **open by design for this demo-scale deployment**; tighten in production if the UI is hosted separately |
| Secrets | None committed: `appsettings.json` holds empty placeholders; real values come from env vars (`docker-compose` `.env`, Actions secrets, gitignored `appsettings.Development.json`) |
| Error leakage | Unknown exceptions → generic 500 message; known validation/state exceptions expose their (non-sensitive) message |

Known hardening notes (non-blocking, tracked in [§14](#14-known-issues--design-notes)):
`VerifyPassword` comparison is not constant-time; CORS is wide open; the JWT secret
once committed to git history should be treated as rotated.

---

## 10. CI/CD & Deployment

### 10.1 Pipeline overview

```text
┌─────────┐   push/PR    ┌──────────────────────────┐
│ Local   │─────────────►│ CI (ci.yml)              │  ubuntu-latest
│ dev     │              │  .NET 10, restore, build,│
└─────────┘              │  test vs PostgreSQL 18   │
                         │  → TRX artifact          │
                         └────────────┬─────────────┘
                                      │ on push to main
                         ┌────────────▼─────────────┐        ┌─────────────────────┐
                         │ CD (cd.yml)              │───────►│ Railway             │
                         │  job 1: docker build+push│        │  serviceInstance-   │
                         │  job 2: Railway redeploy │        │  Redeploy (GraphQL) │
                         └──────────────────────────┘        └─────────────────────┘
```

### 10.2 CI — `.github/workflows/ci.yml`

- **Triggers:** push to `main` or `develop`; pull requests to `main`.
- **Runner:** `ubuntu-latest` with a **PostgreSQL 18 service container**
  (`postgres:18`, db `task_scheduler`, healthchecked with `pg_isready`).
- **Steps:**
  1. `actions/checkout@v4`
  2. `actions/setup-dotnet@v4` → `10.0.x`
  3. `dotnet restore Task-scheduler-system.sln`
  4. `dotnet build Task-scheduler-system.sln --no-restore -c Release`
  5. `dotnet test … -c Release` with:
     - `--logger "trx;LogFileName=results.xml" --results-directory ./TestResults`
     - env: `ConnectionStrings__DefaultConnection` (localhost Postgres), `Jwt__Secret`
       (long test key), `Jwt__Issuer`, `Jwt__Audience`, `ASPNETCORE_ENVIRONMENT=Testing`
     - `Testing` environment ⇒ no auto-migration, no EF/Hangfire registration from
       `AddInfrastructure`, Swagger & Hangfire dashboard off, and
       `appsettings.Testing.json` supplies the test JWT values
  6. `actions/upload-artifact@v4` → `test-results` (`./TestResults/*.xml`), `if: always()`
- **Note:** the solution only registers `Domain.Tests` + `Api.Tests`, so CI runs those
  two suites (see [§12](#12-automated-testing)).

### 10.3 CD — `.github/workflows/cd.yml`

- **Trigger:** push to `main` only.
- **Job 1 — `docker`** (permissions: `contents: read`, `packages: write`):
  1. `docker/login-action@v3` → `ghcr.io` with the automatic `GITHUB_TOKEN` (no manual secret)
  2. `docker/build-push-action@v5` → builds the multi-stage `Dockerfile` and pushes:
     - `ghcr.io/<owner>/task-scheduler-api:latest`
     - `ghcr.io/<owner>/task-scheduler-api:<commit-sha>` (traceable per release)
- **Job 2 — `deploy`** (`needs: docker`):
  - Calls the **Railway GraphQL API** (`https://backboard.railway.app/graphql/v2`) with
    `mutation { serviceInstanceRedeploy(environmentId, serviceId) }`, authenticated with
    the **`RAILWAY_TOKEN`** secret.
  - Railway then pulls the fresh image for the connected service and redeploys it.
  - Fixed `environmentId` / `serviceId` are hardcoded in the workflow (tied to this
    project's Railway environment/service).

**GitHub Actions secrets required:** `RAILWAY_TOKEN` (Railway API token). Nothing else
— GHCR auth uses the built-in `GITHUB_TOKEN`.

### 10.4 Docker & compose

**`Dockerfile`** (multi-stage):

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build     # restore (csproj-only layer first) + publish
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "TaskScheduler.Api.dll"]
```

`.dockerignore` keeps `bin/`, `obj/`, `.git/`, `Docs/`, `Tests/`, `.env*` out of the build context.

**`docker-compose.yml`** (production-shaped):

| Service | Image / build | Key config |
|---|---|---|
| `postgres` | `postgres:18` | `task_scheduler` db, password from `${DB_PASSWORD}`, named volume `postgres_data`, healthcheck `pg_isready`, port 5432 |
| `api` | `ghcr.io/huuquandev/task-scheduler-api:latest` (pull) **and** `build: .` (local fallback) | port `8080:8080`, `ASPNETCORE_ENVIRONMENT=Production`, all secrets injected as env vars from `.env`, `depends_on: postgres: service_healthy` |

**`docker-compose.override.yml`** (auto-merged by `docker compose`, local dev only):
switches the API to `ASPNETCORE_ENVIRONMENT=Development` so Swagger is served.
It must **not** be used on a production server (use `--no-override` or rename it there).

**Production compose usage:**

```bash
cp .env.example .env && $EDITOR .env     # DB_PASSWORD, JWT_SECRET, SMTP_*, NOTIFICATIONS_ADMIN_EMAIL
docker compose pull && docker compose up -d
docker compose logs -f api
```

### 10.5 Environments matrix

| Environment | Who | Swagger | Hangfire dashboard | Auto-migrate | EF/Hangfire DI |
|---|---|---|---|---|---|
| `Development` | local `dotnet run` / compose override | ✅ | ✅ (loopback) | ✅ | ✅ |
| `Production` | compose default / container | ❌ | ✅ (loopback only) | ✅ | ✅ |
| `Testing` | CI + `WebApplicationFactory` | ❌ | ❌ | ❌ | ❌ (tests wire their own) |

### 10.6 Operations runbook

| Task | How |
|---|---|
| Verify the service | `GET /health` (200 + `npgsql: Healthy`) |
| App logs | `docker compose logs -f api` (Serilog stdout) and `logs/log-YYYYMMDD.txt` in the app volume |
| Inspect/repair jobs | `GET /hangfire` from the host machine (loopback-only): Recurring page shows job id = task GUID, next run, cron |
| Deploy a new version | Push to `main` → CD builds & pushes image → Railway redeploys; or on a self-hosted box: `docker compose pull && docker compose up -d` |
| Roll back | Retagged images exist per commit: `ghcr.io/<owner>/task-scheduler-api:<sha>` — point the service at the previous sha |
| Database | Migrations auto-run at boot (idempotent). No destructive ops on restart; `postgres_data` volume persists |
| After a restart | Recurring jobs are in Postgres → schedules resume automatically. Tasks that were `Pending` stay `Pending` until activated |
| Alerting today | SMTP email to `Notifications:AdminEmail` on retry exhaustion; everything else is logs + `/hangfire` + `/api/v1/dashboard` |

---

## 11. Testing the Full Lifecycle (End-to-End Walkthrough)

A complete manual test covering create → activate → trigger → logs → pause → resume,
plus the failure & retry scenario. Works with Swagger UI, Postman or curl.

**Step 0 — authenticate**

```bash
BASE=http://localhost:8080

curl -s -X POST $BASE/api/v1/auth/register -H 'Content-Type: application/json' -d '{
  "username": "devuser", "email": "dev@test.com",
  "password": "Test@1234", "confirmPassword": "Test@1234"
}'

TOKEN=$(curl -s -X POST $BASE/api/v1/auth/login -H 'Content-Type: application/json' \
  -d '{ "username": "devuser", "password": "Test@1234" }' | jq -r .data.token)
AUTH="Authorization: Bearer $TOKEN"
```

**Step 1 — create** (state: `Pending`, no Hangfire job yet)

```bash
curl -s -X POST $BASE/api/v1/tasks -H "$AUTH" -H 'Content-Type: application/json' -d '{
  "name": "My First Task",
  "description": "lifecycle test",
  "cronExpression": "* * * * *",
  "command": "echo Hello Task",
  "maxRetries": 2
}'
# → { "code": 0, ..., "data": "<TASK_ID>" }   (keep TASK_ID)
# Hangfire: no recurring job yet
```

**Step 2 — activate** (`Pending` → `Active`; recurring job appears in `/hangfire` with id = TASK_ID)

```bash
curl -s -X POST $BASE/api/v1/tasks/$TASK_ID/activate -H "$AUTH"
```

**Step 3 — trigger manually** (runs immediately via a background job; rate limit 10/min)

```bash
curl -s -X POST $BASE/api/v1/tasks/$TASK_ID/trigger -H "$AUTH"
```

**Step 4 — read the execution log**

```bash
curl -s $BASE/api/v1/tasks/$TASK_ID/logs -H "$AUTH"
# → [{ id, startedAt, finishedAt, status: "Success", errorMessage: null }]
curl -s $BASE/api/v1/tasks/$TASK_ID/log/<LOG_ID> -H "$AUTH"
# → + durationMs, taskName, exitCode stored on the row
```

**Step 5 — pause / resume** (`Active` → `Paused` → `Active`; job removed & re-registered)

```bash
curl -s -X POST $BASE/api/v1/tasks/$TASK_ID/pause  -H "$AUTH"
curl -s -X POST $BASE/api/v1/tasks/$TASK_ID/resume -H "$AUTH"
```

**Step 6 — failure & retry scenario**

```bash
curl -s -X POST $BASE/api/v1/tasks -H "$AUTH" -H 'Content-Type: application/json' -d '{
  "name": "Failing Task", "description": "retry test",
  "cronExpression": "0 0 1 1 *",
  "command": "exit 1",
  "maxRetries": 1
}'
# activate + trigger the new id
```

Expected sequence (`maxRetries = 1`):

1. Trigger run #1 fails → `RetryCount = 1 ≤ 1` → task stays `Active`, delayed retry **60 s** later
2. Retry run #2 fails → `RetryCount = 2 > 1` → task `Failed`, `TaskFailedEvent` →
   error log + email to `Notifications:AdminEmail` (when configured)
3. Both runs visible in `GET /tasks/{id}/logs` with `status: "Failed"` and the stderr message

**Step 7 — monitor**

```bash
curl -s $BASE/health -H "$AUTH"            # {"status":"Healthy", ...}
curl -s $BASE/api/v1/dashboard -H "$AUTH"  # totals & success/failure counters
# UI: / (dashboard), /tasks.html, /logs.html, /hangfire (loopback)
```

**Step 8 — clean up**

```bash
curl -s -X DELETE $BASE/api/v1/tasks/$TASK_ID -H "$AUTH"   # soft delete + unschedule
```

---

## 12. Automated Testing

**Stack:** xUnit 2.9.3 · FluentAssertions 8.10 · Moq 4.20.72 · coverlet ·
EF Core InMemory & SQLite · Hangfire.InMemory · `Microsoft.AspNetCore.Mvc.Testing`
(`WebApplicationFactory`). **No Testcontainers** — infrastructure tests use
in-memory/SQLite databases.

| Project | Registered in `.sln` | What it covers |
|---|---|---|
| `TaskScheduler.Domain.Tests` | ✅ | Entity state transitions & guards, `CronExpression` value object, domain events; builders (`ScheduledTaskBuilder`, `ExecutionLogBuilder`, `UserBuilder`) |
| `TaskScheduler.Application.Tests` | ❌ | All CQRS handlers (happy + guard paths), validators, `LoggingBehavior`/`ValidationBehavior`, mappers; `BaseTest` + `MapperFixture` |
| `TaskScheduler.Infrastructure.Tests` | ❌ | `TaskRepository`/`TaskExecutionLogRepository`/`UserRepository` (SQLite/InMemory), `HangfireSchedulerService`, `TaskJob`, `TokenService`, event-handler dispatch |
| `TaskScheduler.Api.Tests` | ✅ | `AuthController` & `TasksController` end-to-end over `WebApplicationFactory` (SQLite), `CustomWebApplicationFactory` injects the Testing config |

```bash
dotnet test                                            # solution-level: Domain.Tests + Api.Tests
dotnet test Tests/TaskScheduler.Application.Tests/TaskScheduler.Application.Tests.csproj
dotnet test Tests/TaskScheduler.Infrastructure.Tests/TaskScheduler.Infrastructure.Tests.csproj
```

CI runs the solution-level suite against a **real PostgreSQL 18** service container
(connection string + JWT env vars injected, `ASPNETCORE_ENVIRONMENT=Testing`) and
uploads the TRX results as the `test-results` artifact.

---

## 13. Database Schema

PostgreSQL, database `task_scheduler`. Three EF Core migrations, applied automatically
at startup (`MigrateAsync`):

1. `20260517174606_InitialCreate` — `ScheduledTasks`, `TaskExecutionLogs`
2. `20260521151203_addUsers` — `Users`
3. `20260623155634_AddExcutionDetailsToTaskTaskExcutionLogs` — `ExitCode` on logs

Plus Hangfire's own tables (`SchemaVersions`, `CrontabSchedule`, `RecurringJobs`,
`Jobs`, `JobHistories`, …) created by `Hangfire.PostgreSql` in the **same database**.

### `ScheduledTasks`

| Column | Type | Constraints |
|---|---|---|
| `Id` | `uuid` | PK |
| `Name` | `varchar(100)` | NOT NULL |
| `Description` | `varchar(256)` | |
| `CronExpression` | `varchar(100)` | NOT NULL (VO stored as its raw string) |
| `Command` | `varchar(256)` | NOT NULL |
| `Status` | `integer` | NOT NULL (enum) |
| `LastRunAt`, `NextRunAt` | `timestamptz` | NULL |
| `RetryCount`, `MaxRetries` | `integer` | NOT NULL |
| `IsDeleted` | `boolean` | NOT NULL |
| `CreatedAt`, `UpdatedAt` | `timestamptz` | NOT NULL |

### `TaskExecutionLogs`

| Column | Type | Constraints |
|---|---|---|
| `Id` | `uuid` | PK |
| `TaskId` | `uuid` | NOT NULL, FK → `ScheduledTasks.Id` **CASCADE** |
| `StartedAt` | `timestamptz` | NOT NULL |
| `FinishedAt` | `timestamptz` | NULL |
| `Status` | `integer` | NOT NULL (enum) |
| `ErrorMessage` | `varchar(1000)` | NULL |
| `DurationMs` | `bigint` | NULL |
| `ExitCode` | `integer` | NULL |
| `ScheduledTaskId` | `uuid` | NULL — second FK → `ScheduledTasks.Id` (navigation property, SET NULL) |

Indexes: `IX_TaskExecutionLogs_TaskId`, `IX_TaskExecutionLogs_ScheduledTaskId`.

> ⚠️ Schema quirk: the log carries **two** foreign keys to the same table
> (`TaskId` scalar + `ScheduledTaskId` navigation). `TaskId` is the one that matters;
> see [§14](#14-known-issues--design-notes).

### `Users`

| Column | Type | Constraints |
|---|---|---|
| `Id` | `uuid` | PK |
| `Username`, `Email`, `PasswordHash` | `text` | NOT NULL |
| `CreatedAt`, `UpdatedAt` | `timestamptz` | NULL |
| `IsActive` | `boolean` | NOT NULL |

No `IEntityTypeConfiguration` — convention-mapped.

---

## 14. Known Issues & Design Notes

Deliberately documented so the next developer knows what is a design decision and what
is a gap:

1. **`Failed` tasks keep running.** When retries are exhausted the recurring Hangfire
   job is **not** removed and `ExecuteTask` has no status guard — every subsequent cron
   tick re-runs the failing command (and re-sends the failure email). Fix ideas:
   `UnscheduleTaskAsync` on the exhaustion path, or a status check at the top of
   `ExecuteTask`.
2. **`PUT /tasks/{id}` reschedules unconditionally.** `UpdateTaskHandler` calls
   `RescheduleTaskAsync` for *any* state — updating a `Pending` task registers a
   recurring job, so it starts running on cron without ever being activated.
3. **`Completed` is unreachable on the execution path.** Successful runs set the task
   back to `Active` (`MarkAsActive`), so `ScheduledTaskStatus.Completed` and
   `TaskCompletedEvent` are never produced by the engine; they exist for completeness.
4. **Frontend/backend mismatch on "Re-activate".** `tasks.html` offers *Re-activate*
   for `Failed` tasks, but `POST /tasks/{id}/activate` only accepts `Pending` — the
   button currently returns 400. Either the handler should also accept `Failed` or the
   button should be removed.
5. **`TaskPausedEvent` has no handler** (raised, silently dropped).
6. **Metrics are no-ops.** `MetricsService.Increment*` do nothing; the seam
   (event → handler → service) is ready for a real exporter (e.g. Prometheus).
7. **`ISmsService` is a `NoOp`** (logs only) — placeholder for a real provider.
8. **Retry/`Paused` race.** A delayed retry job scheduled before a pause still fires
   and runs the paused task (`ExecuteTask` has no status guard — same root cause as #1).
9. **`VerifyPassword` is not constant-time** (early return on first byte mismatch).
   PBKDF2 itself is fine; the comparison should use
   `CryptographicOperations.FixedTimeEquals` for hardening.
10. **Dashboard `deletedTasks` is always 0** — the repository filters soft-deleted
    rows, so the count can never be non-zero.
11. **UI reads `command` / `maxRetries`** in the task detail modal, but `TaskDto`
    doesn't expose them → those fields render empty from the API.
12. **`TaskExecutionLog` dual FK** (`TaskId` + `ScheduledTaskId`) — one is enough;
    the redundant navigation FK predates the scalar one.
13. **`Application.Tests` & `Infrastructure.Tests` are not in the `.sln`** — CI only
    runs `Domain.Tests` + `Api.Tests`. Adding the two projects to the solution is a
    one-liner each and extends CI coverage.
14. **CORS is `AllowAll`** — fine for this deployment shape, but should be scoped if
    the UI ever moves to a separate origin.
15. Minor: `MarkAsCompleted` doesn't bump `UpdatedAt` (all other mutators do);
    `TaskRepository.GetPagedAsync` applies the status filter twice (harmless);
    `ExecutionLogDto.FinishedAt` is non-nullable while the column is nullable
    (AutoMapper yields `default(DateTime)` for in-flight runs).

**Roadmap candidates** (from project history): Outbox pattern, Prometheus/Grafana,
SignalR live dashboard, Kafka integration, distributed locking, microservice split.

---

*Generated from the codebase as of the `main` branch — when behavior changes, update
this document in the same PR.*
