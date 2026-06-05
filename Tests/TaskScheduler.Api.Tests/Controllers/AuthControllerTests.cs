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
using TaskScheduler.Application.Common.Models;
using Xunit;

namespace TaskScheduler.Api.Tests.Controllers
{
    public class AuthControllerTests : ApiTestBase
    {

        public AuthControllerTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task Register_Should_Return_Ok_And_UserId()
        {
            // Arrange

            var request = new RegisterCommand(
                "testuser",
                "testuser@example.com",
                "Password123!",
                "Password123!");

            // Act

            var response = await Client.PostAsJsonAsync("/api/v1/auth/register", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var userId = await response.Content.ReadFromJsonAsync<ApiResponse<Guid>>();

            userId.Should().NotBeNull();
            userId.Code.Should().Be(0);
            userId.Message.Should().Be("Registration successful.");
            userId.Data.Should().NotBe(Guid.Empty);
        }

        [Fact]
        public async Task Register_Should_Return_BadRequest_For_Duplicate_Username()
        {
            // Arrange

            var request1 = new RegisterCommand(
                "duplicateuser",
                "duplicateuser@example.com",
                "Password123!",
                "Password123!");    

            var request2 = new RegisterCommand(
                "duplicateuser",
                "duplicateuser2@example.com",
                "Password123!",
                "Password123!");

            // Act
            await Client.PostAsJsonAsync("/api/v1/auth/register", request1);
            var response = await Client.PostAsJsonAsync("/api/v1/auth/register", request2);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Register_Should_Return_BadRequest_For_Duplicate_Email()
        {
            // Arrange

            var request1 = new RegisterCommand(
                "user1",
                "duplicate@example.com",
                "Password123!",
                "Password123!");

            var request2 = new RegisterCommand(
                "user2",
                "duplicate@example.com",
                "Password123!",
                "Password123!");

            // Act
            await Client.PostAsJsonAsync("/api/v1/auth/register", request1);
            var response = await Client.PostAsJsonAsync("/api/v1/auth/register", request2);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Login_Should_Return_Ok_And_Token()
        {
            // Arrange

            var registerRequest = new RegisterCommand(
                "loginuser",
                "loginuser@example.com",
                "Password123!",
                "Password123!");

            // Act
            await Client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);
            var loginRequest = new LoginCommand(
                "loginuser",
                "Password123!");
            var response = await Client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var authResponse = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
            authResponse.Should().NotBeNull();
            authResponse.Data.Token.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task Login_Should_Return_Unauthorized_For_Invalid_Credentials()
        {
            // Arrange

            var loginRequest = new LoginCommand(
                "nonexistentuser",
                "WrongPassword!");

            // Act
            var response = await Client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }
}