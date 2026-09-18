# Enterprise Task Scheduler System

![CI](https://github.com/huuquandev/Task-Scheduler-System/actions/workflows/ci.yml/badge.svg)
![CD](https://github.com/huuquandev/Task-Scheduler-System/actions/workflows/cd.yml/badge.svg)

A scalable backend platform for **scheduling, executing and monitoring background tasks** (arbitrary shell commands) on **cron** schedules. Built with ASP.NET Core 10 and **Clean Architecture**, featuring CQRS (MediatR), Hangfire + PostgreSQL, JWT authentication, a built-in web UI, Docker deployment and GitHub Actions CI/CD with automatic deployment to Railway.

> 📚 For the complete deep-dive — domain model, task lifecycle, full API reference, execution & retry mechanics, CI/CD pipeline and deployment runbook — see **[Docs/ARCHITECTURE.md](Docs/ARCHITECTURE.md)** and the interactive **[Project Guide](Docs/project-guide.html)**.

---

## ✨ Features

### Task management
- Create, read, update and delete tasks (soft delete)
- Cron-based scheduling (`*/5 * * * *` style, validated with [Cronos](https://github.com/mattfarina/cronos))
- Full lifecycle control: **activate → run → pause / resume → trigger manually**
- Paged list with status filtering

### Execution & reliability
- Runs any OS shell command (`cmd.exe` on Windows, `/bin/sh` on Linux)
- Captures **stdout / stderr**, **exit code** and **duration** of every run
- Per-run execution logs, browsable from the API and the web UI
- **Automatic retry with exponential backoff** (60 s → 120 s → 240 s, capped at 300 s) up to a configurable `maxRetries`
- Execution timeout (default 300 s) with full process-tree kill

### Notifications & observability
- **Email alert to an admin** whenever a task exhausts its retries (SMTP)
- Domain events (`TaskCreated`, `TaskPaused`, `TaskFailed`, `TaskCompleted`) dispatched on save
- Correlation IDs on every request and log line
- OpenTelemetry tracing (per-request spans, slow-request detection > 1 s)
- Serilog structured logging (console + rolling daily file)
- `GET /health` endpoint with PostgreSQL liveness probe

### Security
- JWT authentication (HMAC-SHA256), register + login endpoints
- Passwords hashed with PBKDF2 (10 000 iterations, SHA-256, per-user salt)
- Rate limiting on the manual-trigger endpoint (10 requests / minute)
- Hangfire dashboard restricted to loopback IPs

### Web UI
- No-framework frontend (plain HTML/JS) served from the same API:
  - **Dashboard** — task stats and recent tasks
  - **Tasks** — full CRUD, lifecycle actions, per-task logs
  - **Execution logs** — filter by task, durations, errors
  - **Login / Register**

---

## 🛠️ Technology Stack

| Area | Technology |
|---|---|
| Language / Runtime | C#, .NET 10, ASP.NET Core |
| Architecture | Clean Architecture, CQRS (MediatR), Repository + Unit of Work |
| Validation | FluentValidation (pipeline behavior) |
| Mapping | AutoMapper |
| Database | PostgreSQL 18 (EF Core 9 + Npgsql) |
| Scheduling | Hangfire (recurring jobs, in-process server, **PostgreSQL job store** — jobs survive restarts) |
| Cron parsing | Cronos (domain) + Hangfire cron (scheduler) |
| Auth | JWT Bearer, PBKDF2 password hashing |
| Email | `System.Net.Mail.SmtpClient` |
| Logging | Serilog (console + file, daily rolling) |
| Tracing | OpenTelemetry (AspNetCore + HttpClient instrumentation, console exporter) |
| API docs | Swagger/OpenAPI (Swashbuckle, annotations, dev-only) |
| Frontend | Vanilla HTML/CSS/JS in `wwwroot` |
| Packaging | Docker (multi-stage), docker compose |
| CI/CD | GitHub Actions → GitHub Container Registry → Railway |
| Testing | xUnit, FluentAssertions, Moq, EF Core InMemory/SQLite, Hangfire.InMemory, WebApplicationFactory |

---

## 🏗️ Architecture

The solution follows **Clean Architecture**: dependencies point strictly inward, the Domain has zero external dependencies.

```text
┌────────────────────────┐
│        Api             │  Controllers, middleware, Swagger, Hangfire dashboard
│  (ASP.NET Core Web)    │  JWT auth, rate limiting, Serilog, OpenTelemetry
└───────────┬────────────┘
            │
┌───────────▼────────────┐      ┌────────────────────────┐
│      Application       │◄─────│    Infrastructure      │
│  CQRS (MediatR),       │      │ EF Core + PostgreSQL   │
│  FluentValidation,     │      │ Hangfire scheduler +   │
│  AutoMapper, behaviors,│      │ job store, execution,  │
│  domain-event handlers │      │ JWT, SMTP, repositories│
└───────────┬────────────┘      └────────────────────────┘
            │
┌───────────▼────────────┐
│        Domain          │  Entities, enums, value objects,
│  (no dependencies)     │  domain events, business rules
└────────────────────────┘
```

| Layer | Responsibility | Depends on |
|---|---|---|
| **Domain** | Entities (`ScheduledTask`, `TaskExecutionLog`, `User`), enums, `CronExpression` value object, domain events, state-transition rules | — |
| **Application** | Use cases as CQRS commands/queries, pipeline behaviors (logging, validation), domain-event notification handlers, DTOs, all service interfaces | Domain |
| **Infrastructure** | EF Core persistence, Hangfire scheduling/execution, JWT & password services, SMTP email, repositories, Unit of Work | Application, Domain |
| **Api** | HTTP endpoints, middleware pipeline, authentication, CORS, rate limiting, health checks, static UI | Application, Infrastructure |

**How a request flows** (example: `POST /api/v1/tasks`):

```text
HTTP request
  → CorrelationId / ExceptionHandling middleware
  → JWT authentication + authorization
  → TasksController
  → MediatR: LoggingBehavior → ValidationBehavior → CreateTaskHandler
  → ScheduledTask (domain, raises TaskCreatedEvent)
  → Repository → ApplicationDbContext (EF Core)
  → SaveChangesAsync → PostgreSQL + dispatch domain events
  → ApiResponse envelope: { "code": 0, "message": "...", "data": <guid> }
```

**How a scheduled run flows** (Hangfire → result):

```text
Hangfire recurring job (cron, job id = task GUID)
  → TaskJob.Execute(taskId)
  → TaskExecutionService.ExecuteTask(taskId)
      1. create TaskExecutionLog (Running)
      2. task → Running, persist
      3. run shell command (cmd.exe / /bin/sh), capture stdout/stderr/exit code
      4. success → log → Success, task → Active, NextRunAt updated, retries reset
         failure → log → Failed, retry scheduled (backoff) or task → Failed
  → domain events → logging / metrics / admin email handlers
```

---

## 📂 Project Structure

```text
Task-scheduler-system/
├── Task-scheduler-system.sln
├── Dockerfile                        # Multi-stage build (SDK → ASP.NET runtime)
├── docker-compose.yml                # PostgreSQL + API (production-shaped)
├── docker-compose.override.yml       # Local-dev overrides (Swagger on)
├── .env.example                      # Template for docker compose secrets
├── .github/workflows/
│   ├── ci.yml                        # Build + test on push/PR
│   └── cd.yml                        # Build & push image to GHCR, redeploy on Railway
├── Docs/
│   ├── ARCHITECTURE.md               # Full project reference (lifecycle, API, CI/CD, deploy)
│   ├── project-guide.html            # Interactive project guide
│   └── Images/                       # Screenshots & diagrams
├── src/
│   ├── TaskScheduler.Domain/         # Entities, enums, events, value objects
│   ├── TaskScheduler.Application/    # CQRS handlers, behaviors, DTOs, interfaces
│   ├── TaskScheduler.Infrastructure/ # EF Core, Hangfire, JWT, SMTP, repositories
│   └── TaskScheduler.Api/            # Controllers, middleware, Program.cs, wwwroot/ (web UI)
└── Tests/
    ├── TaskScheduler.Domain.Tests/
    ├── TaskScheduler.Application.Tests/
    ├── TaskScheduler.Infrastructure.Tests/
    └── TaskScheduler.Api.Tests/
```

---

## 🚀 Getting Started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Docker + Docker Compose (for the bundled PostgreSQL)
- A `git` checkout of this repository

### Option A — Docker Compose (fastest)

```bash
# 1. Configure secrets
cp .env.example .env        # Windows: Copy-Item .env.example .env
#    then edit .env: DB_PASSWORD, JWT_SECRET (>= 32 chars), SMTP_*, NOTIFICATIONS_ADMIN_EMAIL

# 2. Build and start PostgreSQL + API
docker compose up -d --build

# 3. Verify
#    Web UI:        http://localhost:8080
#    Swagger:       http://localhost:8080/swagger   (local override sets Development)
#    Health:        http://localhost:8080/health
#    Hangfire:      http://localhost:8080/hangfire  (loopback only)
```

### Option B — Local development (`dotnet run`)

```bash
# 1. Start only the database
docker compose up postgres -d

# 2. Create src/TaskScheduler.Api/appsettings.Development.json (gitignored)
#    with your real connection string, JWT secret, SMTP settings:
#    {
#      "ConnectionStrings": { "DefaultConnection": "Host=localhost;Port=5432;Database=task_scheduler;Username=postgres;Password=YOUR_PASSWORD" },
#      "Jwt": { "Secret": "YOUR_SECRET_AT_LEAST_32_CHARS" }
#    }

# 3. Run (EF Core migrations apply automatically at startup)
dotnet run --project src/TaskScheduler.Api/TaskScheduler.Api.csproj
#    Swagger: https://localhost:7233/swagger
```

### Option C — Plain `dotnet` build & test

```bash
dotnet restore
dotnet build -c Release
dotnet test                      # runs all four test projects (all registered in the .sln)
```

> All four test projects (`Domain.Tests`, `Application.Tests`, `Infrastructure.Tests`, `Api.Tests`) are registered in the `.sln`. To run a specific project explicitly:
> `dotnet test Tests/TaskScheduler.Application.Tests/TaskScheduler.Application.Tests.csproj`

### First steps with the API

1. `POST /api/v1/auth/register` — create a user
2. `POST /api/v1/auth/login` — get a JWT
3. `POST /api/v1/tasks` — create a task (`cronExpression: "* * * * *"` runs every minute)
4. `POST /api/v1/tasks/{id}/activate` — register it with Hangfire
5. `POST /api/v1/tasks/{id}/trigger` — run it immediately
6. `GET /api/v1/tasks/{id}/logs` — inspect the execution log

A complete end-to-end walkthrough (including the failure & retry scenario) is in [Docs/ARCHITECTURE.md → Testing the full lifecycle](Docs/ARCHITECTURE.md#11-testing-the-full-lifecycle-end-to-end-walkthrough).

---

## 🔧 Configuration

All sensitive values are injected via **environment variables** — never committed. `appsettings.json` holds structure with empty secret placeholders; `appsettings.Development.json` (gitignored) holds local values; docker compose / CI inject the rest.

| Variable | Used by | Description |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | EF Core + Hangfire | PostgreSQL connection string (one database serves both app data and the Hangfire job store) |
| `Jwt__Secret` | JWT | HMAC-SHA256 signing key, **≥ 32 chars** (`openssl rand -base64 48`) |
| `Jwt__Issuer` / `Jwt__Audience` | JWT | `TaskSchedulerAPI` / `TaskSchedulerClient` |
| `Jwt__ExpireHours` | JWT | Token lifetime (default **72 h**) |
| `TaskExecution__TimeoutSeconds` | Execution | Per-run command timeout (default **300 s**) |
| `Smtp__Host` / `Smtp__Port` / `Smtp__User` / `Smtp__Password` | Email alerts | SMTP server (e.g. Gmail + app password) |
| `Notifications__AdminEmail` | Email alerts | Recipient for task-failure alerts (empty = no email) |
| `ASPNETCORE_ENVIRONMENT` | App | `Development` (Swagger on), `Production`, `Testing` |

Docker compose variables (from `.env`, see `.env.example`): `DB_PASSWORD`, `JWT_SECRET`, `SMTP_HOST`, `SMTP_PORT`, `SMTP_USER`, `SMTP_PASSWORD`, `NOTIFICATIONS_ADMIN_EMAIL`.

---

## 📡 API Overview

All responses use a common envelope: `{ "code": int, "message": string, "data": T }`. All `/tasks`, `/dashboard` endpoints require `Authorization: Bearer <jwt>`. The full reference (request/response DTOs, status rules, error mapping) is in [Docs/ARCHITECTURE.md](Docs/ARCHITECTURE.md#6-api-reference).

| Method | Route | Description |
|---|---|---|
| POST | `/api/v1/auth/register` | Create a user (public) |
| POST | `/api/v1/auth/login` | Authenticate, receive JWT (public) |
| POST | `/api/v1/tasks` | Create a task (starts `Pending`) |
| GET | `/api/v1/tasks` | List all tasks |
| GET | `/api/v1/tasks/paged?page=&pageSize=&status=` | Paged list, optional status filter |
| GET | `/api/v1/tasks/{id}` | Task details |
| PUT | `/api/v1/tasks/{id}` | Partial update (name, description, cron, command, maxRetries) |
| DELETE | `/api/v1/tasks/{id}` | Soft delete + unschedule |
| POST | `/api/v1/tasks/{id}/activate` | `Pending`/`Failed` → `Active`, registers the recurring job (Failed: resets retry count) |
| POST | `/api/v1/tasks/{id}/pause` | `Active` → `Paused`, removes the recurring job |
| POST | `/api/v1/tasks/{id}/resume` | `Paused` → `Active`, re-registers the recurring job |
| POST | `/api/v1/tasks/{id}/trigger` | Run immediately (rate-limited: 10/min) |
| GET | `/api/v1/tasks/{id}/logs` | Execution logs for a task |
| GET | `/api/v1/tasks/{id}/log/{logId}` | Single execution log detail |
| GET | `/api/v1/dashboard` | Aggregate stats (totals, active, paused, running, success/fail counts) |
| GET | `/health` | Health check (+ PostgreSQL probe) |

---

## 🔄 Task Lifecycle at a Glance

```text
                 POST /tasks
                      │
                      ▼
    ┌────────┐  activate   ┌────────┐  pause   ┌────────┐
    │Pending │────────────►│ Active │─────────►│ Paused │
    └────────┘             └───┬────┘          └───┬────┘
        ▲                      │  ▲                 │ resume
        │                      ▼  │                 │
        │              ┌────────┐  │                 ▼
        │              │Running │  │            (back to Active)
        │              └───┬────┘  │
        │         success   │      │
        │                   ▼      │
        │            (back to Active)  failure
        │                   │      │
        │            retry within maxRetries?──yes──► delayed job (60s, 120s, 240s, 300s cap)
        │                   │no
        │                   ▼
        │               ┌────────┐
        └────────────────│ Failed│  → TaskFailedEvent → error log + admin email
                          └────────┘    recurring job removed (re-activate to restart)
```

| State | Meaning |
|---|---|
| `Pending` | Created, not yet registered with Hangfire |
| `Active` | Recurring job registered; waiting for its cron slot |
| `Running` | An execution is in progress |
| `Paused` | Recurring job removed; task kept, not scheduled |
| `Failed` | Retries exhausted after a failed run; recurring job removed until re-activated |
| `Completed` | Execution finished successfully (see [design notes](Docs/ARCHITECTURE.md#14-known-issues--design-notes)) |

Full transition rules, guards and edge cases: [Docs/ARCHITECTURE.md → Task lifecycle](Docs/ARCHITECTURE.md#4-task-lifecycle).

---

## 🧪 Testing

| Project | Covers | Tools |
|---|---|---|
| `TaskScheduler.Domain.Tests` | Entities, state transitions, `CronExpression` value object, domain events | xUnit, FluentAssertions, builders |
| `TaskScheduler.Application.Tests` | CQRS handlers, validators, MediatR pipeline behaviors | xUnit, Moq, FluentValidation, AutoMapper |
| `TaskScheduler.Infrastructure.Tests` | Repositories, Hangfire scheduler, `TaskJob`, `TokenService` | xUnit, Moq, EF Core InMemory/SQLite, Hangfire.InMemory |
| `TaskScheduler.Api.Tests` | Controllers & API endpoints via `WebApplicationFactory` | xUnit, Moq, Mvc.Testing, SQLite |

```bash
dotnet test                                   # all four test projects
dotnet test Tests/<Project>/<Project>.csproj  # any specific test project
```

CI runs the suite against a real **PostgreSQL 18** service container and uploads the TRX results as a build artifact.

---

## ⚙️ CI/CD Pipeline

```text
git push / PR ──► CI (build + test on PostgreSQL service) ──► on main: CD ──► GHCR image ──► Railway redeploy
```

**CI** (`.github/workflows/ci.yml`) — on push to `main`/`develop` and PRs to `main`:
1. Checkout, setup .NET 10
2. `dotnet restore` → `dotnet build -c Release`
3. `dotnet test -c Release` with a **PostgreSQL 18 service container** (real DB, no mocks) and `ASPNETCORE_ENVIRONMENT=Testing`
4. Upload TRX test results as an artifact

**CD** (`.github/workflows/cd.yml`) — on push to `main` only:
1. `docker` job: multi-stage Docker build, pushed to **GHCR** as `ghcr.io/<owner>/task-scheduler-api:latest` **and** `:<commit-sha>` (authenticated with the automatic `GITHUB_TOKEN`)
2. `deploy` job: triggers a **Railway** `serviceInstanceRedeploy` via the GraphQL API (`RAILWAY_TOKEN` secret) — Railway pulls the new image and redeploys

The full pipeline walkthrough, secrets and failure modes: [Docs/ARCHITECTURE.md → CI/CD & Deployment](Docs/ARCHITECTURE.md#10-cicd--deployment).

---

## 🐳 Deployment

### Image
`Dockerfile` is multi-stage: `mcr.microsoft.com/dotnet/sdk:10.0` (restore + publish) → `mcr.microsoft.com/dotnet/aspnet:10.0` (runtime only). Listens on **port 8080**.

### docker compose (self-host)
`docker-compose.yml` runs **PostgreSQL 18** (named volume, healthchecked) + the **API** (image pulled from GHCR, or built locally for offline dev). It sets `ASPNETCORE_ENVIRONMENT=Production` and injects all secrets from `.env`. `docker-compose.override.yml` (auto-merged locally) switches the environment to `Development` so Swagger is available.

```bash
cp .env.example .env && $EDITOR .env
docker compose pull && docker compose up -d   # on a server, after a CD push
```

### Railway (this project's production target)
The service is connected to this repository on Railway; after each CD push the workflow asks Railway to redeploy, which pulls the fresh `:latest` image. Migrations run automatically at container startup.

**Operational notes**
- Database schema: **EF Core migrations run automatically at startup** (skipped in the `Testing` environment)
- Hangfire jobs persist in PostgreSQL, so schedules **survive restarts**; tasks in `Pending` state must be activated again only if their job was never registered
- Logs: container stdout (Serilog) + `logs/log-YYYYMMDD.txt` inside the app
- Monitoring: `GET /health`, `GET /hangfire` (from the host's loopback), `GET /api/v1/dashboard`

---

## 📷 Screenshots

| Swagger UI | Hangfire Dashboard |
|---|---|
| ![Swagger](Docs/Images/swagger.png) | ![Hangfire](Docs/Images/hangfire.png) |

| Architecture | Database schema |
|---|---|
| ![Architecture](Docs/Images/architecture.png) | ![Database](Docs/Images/database.png) |

---

## 📖 Documentation

| Document | Contents |
|---|---|
| [Docs/ARCHITECTURE.md](Docs/ARCHITECTURE.md) | Full reference: architecture, domain model, task lifecycle & transitions, execution & retry mechanics, domain events, API reference, database schema, security, configuration, testing, CI/CD, deployment, operations, known issues |
| [Docs/project-guide.html](Docs/project-guide.html) | Interactive single-page guide (architecture, lifecycle, CI/CD, local setup, end-to-end task walkthrough) |

---

## 👨‍💻 Author

**Huu Quan** — Backend .NET Developer

- GitHub: https://github.com/huuquandev
- LinkedIn: https://www.linkedin.com/in/ph%E1%BA%A1m-h%E1%BB%AFu-qu%C3%A2n-291191419/

---

## ⭐ If you find this project useful, please give it a star.
