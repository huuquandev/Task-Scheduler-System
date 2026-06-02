using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using TaskScheduler.Infrastructure.Tests.Common;
using TaskScheduler.Infrastructure.Persistence.Repositories;
using Xunit;
namespace TaskScheduler.Infrastructure.Tests.Persistence
{
    public class UserRepositoryTests : BaseInfrastructureTest
    {
        [Fact]
        public async Task GetByUsernameAsync_ReturnsUser_WhenUserExists()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                Email = "testuser@example.com",
                PasswordHash = "hashedpassword"
            };

            // DbContext #1 → seed/setup
            using (var seedContext = Factory.CreateDbContext())
            {
                var repository = new UserRepository(seedContext);

                await repository.AddAsync(user);

                await seedContext.SaveChangesAsync();
            }
            // DbContext #2 → assert/query DB 
            using (var assertContext = Factory.CreateDbContext())
            {
                var repository = new UserRepository(assertContext);

                // Act
                var result = await repository.GetByUsernameAsync("testuser");

                // Assert
                result.Should().NotBeNull();
                result.Username.Should().Be("testuser");
                result.Email.Should().Be("testuser@example.com");
                result.PasswordHash.Should().Be("hashedpassword");
            }
        }

        [Fact]
        public async Task GetByEmailAsync_ReturnsUser_WhenUserExists()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                Email = "testuser@example.com",
                PasswordHash = "hashedpassword"
            };

            // DbContext #1 → seed/setup
            using (var seedContext = Factory.CreateDbContext())
            {
                var repository = new UserRepository(seedContext);

                await repository.AddAsync(user);

                await seedContext.SaveChangesAsync();
            }
            // DbContext #2 → assert/query DB 
            using (var assertContext = Factory.CreateDbContext())
            {
                var repository = new UserRepository(assertContext);
                
                // Act
                var result = await repository.GetByEmailAsync("testuser@example.com");

                // Assert
                result.Should().NotBeNull();
                result.Username.Should().Be("testuser");
                result.Email.Should().Be("testuser@example.com");
                result.PasswordHash.Should().Be("hashedpassword");
            }
        }

        [Fact]
        public async Task AddAsync_Should_Save_User()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                Email = "testuser@example.com",
                PasswordHash = "hashedpassword"
            };

            // DbContext #1 → seed/setup
            using (var seedContext = Factory.CreateDbContext())
            {
                var repository = new UserRepository(seedContext);

                await repository.AddAsync(user);

                await seedContext.SaveChangesAsync();
            }
            // DbContext #2 → assert/query DB 
            using (var assertContext = Factory.CreateDbContext())
            {
                var repository = new UserRepository(assertContext);
                
                // Act
                var savedUser = await assertContext.Users.FirstOrDefaultAsync(x => x.Id == user.Id);

                // Assert
                savedUser.Should().NotBeNull();
                savedUser.Username.Should().Be("testuser");
                savedUser.Email.Should().Be("testuser@example.com");
                savedUser.PasswordHash.Should().Be("hashedpassword");
            }
        }
    }
}