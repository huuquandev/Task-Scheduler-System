# Task Scheduler System

## Overview

Task Scheduler is a backend platform designed to schedule, execute, and monitor background jobs.

## Features

- User Authentication & Authorization
- Create Scheduled Tasks
- Execute Background Jobs
- Task Execution Logs
- Retry Failed Jobs
- Task History

## Tech Stack

- ASP.NET Core
- Entity Framework Core
- SQL Server
- Redis
- Hangfire
- MediatR
- CQRS
- Docker

## Architecture

![Architecture](docs/architecture.png)

## How to Run

```bash
docker-compose up -d
```

## Project Structure

```text
src/
├── API
├── Application
├── Domain
├── Infrastructure
```
