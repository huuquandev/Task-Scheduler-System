using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using TaskScheduler.Infrastructure.Services;
using TaskScheduler.Domain.Entities;
using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using Xunit;
using System.Security.Claims;
namespace TaskScheduler.Infrastructure.Tests.Services
{
    public class TokenServiceTests
    {
        private readonly TokenService _tokenService;
        public TokenServiceTests()
        {
            var inMemorySettings = new Dictionary<string, string> {
                {"Jwt:Secret", "ThisIsASecretKeyForTestingPurposesOnly!"},
                {"Jwt:Issuer", "TestIssuer"},
                {"Jwt:Audience", "TestAudience"},
                {"Jwt:ExpireHours", "1"}
            };

            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            _tokenService = new TokenService(configuration);
        }

        [Fact]
        public void HashPassword_ShouldReturnDifferentPlaintext()
        {
            // Arrange
            string password = "TestPassword123!";

            // Act
            string hash = _tokenService.HashPassword(password);

            // Assert
            hash.Should().NotBe("TestPassword123!");
        }

        [Fact]
        public void HashPassword_ShouldReturnDifferentHashesForSamePassword()
        {
            // Arrange
            string password = "TestPassword123!";

            // Act
            string hash1 = _tokenService.HashPassword(password);
            string hash2 = _tokenService.HashPassword(password);

            // Assert
            hash1.Should().NotBe(hash2);
        }

        [Fact]
        public void HashPassword_ShouldReturnHash()
        {
            // Arrange
            string password = "TestPassword123!";

            // Act
            string hash = _tokenService.HashPassword(password);

            // Assert
            hash.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void VerifyPassword_ShouldReturnTrueForCorrectPassword()
        {
            // Arrange
            string password = "TestPassword123!";
            string hash = _tokenService.HashPassword(password);

            // Act
            bool result = _tokenService.VerifyPassword(password, hash);

            // Assert
            result.Should().BeTrue();
        }
        
        [Fact]
        public void VerifyPassword_ShouldReturnFalseForIncorrectPassword()
        {
            // Arrange
            string password = "TestPassword123!";
            string hash = _tokenService.HashPassword(password);

            // Act
            bool result = _tokenService.VerifyPassword("WrongPassword", hash);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void GenerateJwtToken_Should_Contain_Username_Claim()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "admin",
                Email = "admin@example.com",
                IsActive = true
            };

            // Act
            var token = _tokenService.GenerateJwtToken(user);

            // Decode token to get claims
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            // Assert
            jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Name && c.Value == user.Username);
        }

        [Fact]
        public void GenerateJwtToken_Should_Have_Future_Expiration()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "admin",
                Email = "admin@example.com",
                IsActive = true
            };

            // Act
            var token = _tokenService.GenerateJwtToken(user);

            // Decode token to get claims
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            // Assert
            jwtToken.ValidTo.Should().BeAfter(DateTime.UtcNow);
        }
    }
}