using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TaskScheduler.Application.Auth.DTOs;
using TaskScheduler.Application.Auth.Commands.AuthLogin;
using TaskScheduler.Application.Auth.Commands.AuthRegister;
using TaskScheduler.Application.Tasks.Queries.GetTaskById;
using TaskScheduler.Application.Tasks.Commands.CreateTask;
using TaskScheduler.Application.Tasks.Commands.UpdateTask;
using TaskScheduler.Application.Tasks.Queries.GetTasks;
using TaskScheduler.Application.Common.Models;
using Xunit;
namespace TaskScheduler.Api.Tests.Controllers
{
    public class TasksControllerTests : ApiTestBase
    {
        public TasksControllerTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        private async Task<string> GetAuthTokenAsync()
        {
            var username = $"user_{Guid.NewGuid():N}";

            var password = "Password123!";

            var email = $"{username}@example.com";

            await Client.PostAsJsonAsync( "/api/v1/auth/register", new RegisterCommand(username, email, password, password));

            var loginResponse = await Client.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand(username, password));

            loginResponse.EnsureSuccessStatusCode();

            var authResponse = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();

            return authResponse!.Data.Token;
        }

        [Fact]
        public async Task GetTasks_WithoutToken_Should_Return_Unauthorized()
        {
            // Act
            var response = await Client.GetAsync("/api/v1/tasks");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetTasks_WithToken_Should_Return_Ok()
        {
            // Arrange
            var token = await GetAuthTokenAsync();

            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await Client.GetAsync("/api/v1/tasks");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task CreateTask_Should_Return_TaskId()
        {
            // Arrange
            var token = await GetAuthTokenAsync();

            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var request = new CreateTaskCommand(
                        "Backup Job",
                        "Daily backup task",
                        "0 * * * *",
                        "backup.exe",
                        3);

            // Act
            var response = await Client.PostAsJsonAsync("/api/v1/tasks", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var taskid = await response.Content.ReadFromJsonAsync<ApiResponse<Guid>>();
            taskid.Data.Should().NotBeEmpty();
        }

        [Fact]
        public async Task CreateTask_InvalidCron_Should_Return_400()
        {
            // Arrange
            var token = await GetAuthTokenAsync();

            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var request = new CreateTaskCommand(
                        "Backup Job",
                        "Daily backup task",
                        "invalid-cron",
                        "backup.exe",
                        3);

            // Act
            var response = await Client.PostAsJsonAsync("/api/v1/tasks", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task GetTaskById_Should_Return_Task()
        {
            // Arrange
            var token = await GetAuthTokenAsync();

            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var createRequest = new CreateTaskCommand(
                        "Backup Job",
                        "Daily backup task",
                        "0 * * * *",
                        "backup.exe",
                        3);

            var createResponse = await Client.PostAsJsonAsync("/api/v1/tasks", createRequest);

            createResponse.EnsureSuccessStatusCode();

            var taskId = await createResponse.Content.ReadFromJsonAsync<ApiResponse<Guid>>();

            // Act
            var response = await Client.GetAsync($"/api/v1/tasks/{taskId.Data}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var task = await response.Content.ReadFromJsonAsync<ApiResponse<TaskDto>>();

            task.Should().NotBeNull();
            task!.Data.Id.Should().Be(taskId.Data);
            task!.Data.Name.Should().Be(createRequest.Name);
            task!.Data.Description.Should().Be(createRequest.Description);
            task!.Data.CronExpression.Should().Be(createRequest.CronExpression);
        }

        [Fact]
        public async Task GetTaskById_NotFound_Should_Return_404()
        {
            // Arrange
            var token = await GetAuthTokenAsync();

            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var nonExistentTaskId = Guid.NewGuid();

            // Act
            var response = await Client.GetAsync($"/api/v1/tasks/{nonExistentTaskId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task UpdateTask_Should_Return_200()
        {
            // Arrange
            var token = await GetAuthTokenAsync();

            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var createRequest = new CreateTaskCommand(
                        "Backup Job",
                        "Daily backup task",
                        "0 * * * *",
                        "backup.exe",
                        3);

            var createResponse = await Client.PostAsJsonAsync("/api/v1/tasks", createRequest);

            createResponse.EnsureSuccessStatusCode();

            var taskId = await createResponse.Content.ReadFromJsonAsync<ApiResponse<Guid>>();

            var updateRequest = new UpdateTaskCommand(
                taskId.Data,
                "Updated Backup Job",
                "Updated description",
                "0 0 * * *",
                "updated_backup.exe",
                5);

            // Act
            var response = await Client.PutAsJsonAsync($"/api/v1/tasks/{taskId.Data}", updateRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        [Fact]
        public async Task DeleteTask_Should_Return_200()
        {
            // Arrange
            var token = await GetAuthTokenAsync();

            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var createRequest = new CreateTaskCommand(
                        "Backup Job",
                        "Daily backup task",
                        "0 * * * *",
                        "backup.exe",
                        3);

            var createResponse = await Client.PostAsJsonAsync("/api/v1/tasks", createRequest);

            createResponse.EnsureSuccessStatusCode();

            var taskId = await createResponse.Content.ReadFromJsonAsync<ApiResponse<Guid>>();

            // Act
            var response = await Client.DeleteAsync($"/api/v1/tasks/{taskId.Data}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task ActiveTask_Should_Return_200()
        {
            // Arrange
            var token = await GetAuthTokenAsync();

            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var createRequest = new CreateTaskCommand(
                        "Backup Job",
                        "Daily backup task",
                        "0 * * * *",
                        "backup.exe",
                        3);

            var createResponse = await Client.PostAsJsonAsync("/api/v1/tasks", createRequest);

            createResponse.EnsureSuccessStatusCode();

            var taskId = await createResponse.Content.ReadFromJsonAsync<ApiResponse<Guid>>();

            // Act
            var response = await Client.PostAsync($"/api/v1/tasks/{taskId.Data}/trigger", new StringContent("", Encoding.UTF8, "application/json"));

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task PauseTask_Should_Return_200()
        {
            // Arrange
            var token = await GetAuthTokenAsync();

            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var createRequest = new CreateTaskCommand(
                        "Backup Job",
                        "Daily backup task",
                        "0 * * * *",
                        "backup.exe",
                        3);

            var createResponse = await Client.PostAsJsonAsync("/api/v1/tasks", createRequest);

            createResponse.EnsureSuccessStatusCode();

            var taskId = await createResponse.Content.ReadFromJsonAsync<ApiResponse<Guid>>();

            // Act
            var response = await Client.PostAsync($"/api/v1/tasks/{taskId.Data}/pause", new StringContent("", Encoding.UTF8, "application/json"));

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task ResumeTask_Should_Return_200()
        {
            // Arrange
            var token = await GetAuthTokenAsync();

            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var createRequest = new CreateTaskCommand(
                        "Backup Job",
                        "Daily backup task",
                        "0 * * * *",
                        "backup.exe",
                        3);

            var createResponse = await Client.PostAsJsonAsync("/api/v1/tasks", createRequest);

            createResponse.EnsureSuccessStatusCode();

            var taskId = await createResponse.Content.ReadFromJsonAsync<ApiResponse<Guid>>();

            // Act
            var response = await Client.PostAsync($"/api/v1/tasks/{taskId.Data}/resume", new StringContent("", Encoding.UTF8, "application/json"));

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task TriggerTask_Should_Return_200()
        {
            // Arrange
            var token = await GetAuthTokenAsync();

            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var createRequest = new CreateTaskCommand(
                        "Backup Job",
                        "Daily backup task",
                        "0 * * * *",
                        "backup.exe",
                        3);

            var createResponse = await Client.PostAsJsonAsync("/api/v1/tasks", createRequest);

            createResponse.EnsureSuccessStatusCode();

            var taskId = await createResponse.Content.ReadFromJsonAsync<ApiResponse<Guid>>();

            // Act
            var response = await Client.PostAsync($"/api/v1/tasks/{taskId.Data}/trigger", new StringContent("", Encoding.UTF8, "application/json"));

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GetExecutionLogs_Should_Return_Logs()
        {
            // Arrange
            var token = await GetAuthTokenAsync();
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var createRequest = new CreateTaskCommand(
                "Backup Job",
                "Daily backup task",
                "0 * * * *",
                "backup.exe",
                3);

            var createResponse = await Client.PostAsJsonAsync("/api/v1/tasks", createRequest);

            createResponse.EnsureSuccessStatusCode();

            var taskId = (await createResponse.Content.ReadFromJsonAsync<ApiResponse<Guid>>())!.Data;

            // Trigger execution
            var triggerResponse = await Client.PostAsync($"/api/v1/tasks/{taskId.Data}/trigger", new StringContent("", Encoding.UTF8, "application/json"));

            triggerResponse.EnsureSuccessStatusCode();

            // Act
            var response = await Client.GetAsync($"/api/v1/tasks/{taskId}/logs");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<ExecutionLogDto>>>();

            result.Should().NotBeNull();
            result!.Data.Should().NotBeEmpty();

            var log = result.Data.First();

            log.TaskId.Should().Be(taskId);
            log.StartedAt.Should().NotBe(default);
            log.Status.Should().NotBeNullOrWhiteSpace();
        }
    }
}