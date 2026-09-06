FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Task-scheduler-system.sln .
COPY src/TaskScheduler.Domain/TaskScheduler.Domain.csproj             src/TaskScheduler.Domain/
COPY src/TaskScheduler.Application/TaskScheduler.Application.csproj   src/TaskScheduler.Application/
COPY src/TaskScheduler.Infrastructure/TaskScheduler.Infrastructure.csproj src/TaskScheduler.Infrastructure/
COPY src/TaskScheduler.Api/TaskScheduler.Api.csproj                   src/TaskScheduler.Api/

RUN dotnet restore src/TaskScheduler.Api/TaskScheduler.Api.csproj

COPY src/ src/
RUN dotnet publish src/TaskScheduler.Api/TaskScheduler.Api.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "TaskScheduler.Api.dll"]
