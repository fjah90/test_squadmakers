using JokesApi.Controllers;
using JokesApi.Data;
using JokesApi.Entities;
using JokesApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using System.Text.Json;

namespace JokesApi.Tests
{
    public class UserControllerTests
    {
        private static AppDbContext CreateDb()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        private static ITokenService CreateTokenService()
        {
            var mockTokenService = new Mock<ITokenService>();
            mockTokenService.Setup(s => s.CreateTokenPair(It.IsAny<User>()))
                .Returns(new TokenPair("test-token", "test-refresh-token"));
            return mockTokenService.Object;
        }

        [Fact]
        public async Task Register_ReturnsCreated_WithValidData()
        {
            // Arrange
            var db = CreateDb();
            var controller = new UserController(db, CreateTokenService(), NullLogger<UserController>.Instance);
            var request = new UserController.RegisterRequest(
                "Test User",
                "test@example.com",
                "Password123!",
                null
            );

            // Act
            var result = await controller.Register(request);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(201, createdResult.StatusCode);
            
            var response = createdResult.Value;
            Assert.NotNull(response);
            
            // Verify user was saved to database
            var savedUser = await db.Users.FirstOrDefaultAsync(u => u.Email == "test@example.com");
            Assert.NotNull(savedUser);
            Assert.Equal("Test User", savedUser.Name);
            Assert.Equal("user", savedUser.Role); // Default role
        }

        [Fact]
        public async Task Register_ReturnsConflict_WhenEmailExists()
        {
            // Arrange
            var db = CreateDb();
            var existingUser = new User
            {
                Id = Guid.NewGuid(),
                Name = "Existing User",
                Email = "existing@example.com",
                PasswordHash = "hash",
                Role = "user"
            };
            db.Users.Add(existingUser);
            await db.SaveChangesAsync();

            var controller = new UserController(db, CreateTokenService(), NullLogger<UserController>.Instance);
            var request = new UserController.RegisterRequest(
                "New User",
                "existing@example.com", // Same email
                "Password123!",
                null
            );

            // Act
            var result = await controller.Register(request);

            // Assert
            var conflictResult = Assert.IsType<ConflictObjectResult>(result);
            Assert.Equal(409, conflictResult.StatusCode);
        }

        [Fact]
        public async Task Register_SetsAdminRole_WhenRequested()
        {
            // Arrange
            var db = CreateDb();
            var controller = new UserController(db, CreateTokenService(), NullLogger<UserController>.Instance);
            var request = new UserController.RegisterRequest(
                "Admin User",
                "admin@example.com",
                "Password123!",
                "admin"
            );

            // Act
            var result = await controller.Register(request);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            
            // Verify user was saved with admin role
            var savedUser = await db.Users.FirstOrDefaultAsync(u => u.Email == "admin@example.com");
            Assert.NotNull(savedUser);
            Assert.Equal("admin", savedUser.Role);
        }

        [Fact]
        public async Task GetAllUsers_ReturnsUsers_WhenAdmin()
        {
            // Arrange
            var db = CreateDb();
            db.Users.Add(new User { Id = Guid.NewGuid(), Name = "User 1", Email = "user1@example.com", PasswordHash = "hash", Role = "user" });
            db.Users.Add(new User { Id = Guid.NewGuid(), Name = "User 2", Email = "user2@example.com", PasswordHash = "hash", Role = "admin" });
            await db.SaveChangesAsync();

            var controller = new UserController(db, CreateTokenService(), NullLogger<UserController>.Instance);
            
            // Act
            var result = await controller.GetAllUsers();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var users = Assert.IsAssignableFrom<IEnumerable<object>>(okResult.Value);
            Assert.Equal(2, users.Count());
        }

        [Fact]
        public async Task GetUserById_ReturnsUser_WhenExists()
        {
            // Arrange
            var db = CreateDb();
            var userId = Guid.NewGuid();
            db.Users.Add(new User { Id = userId, Name = "Test User", Email = "test@example.com", PasswordHash = "hash", Role = "user" });
            await db.SaveChangesAsync();

            var controller = new UserController(db, CreateTokenService(), NullLogger<UserController>.Instance);
            
            // Act
            var result = await controller.GetUserById(userId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            // Convertir a JSON y luego a diccionario para acceder a las propiedades
            var json = JsonSerializer.Serialize(okResult.Value);
            var userDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
            
            Assert.NotNull(userDict);
            Assert.Equal("Test User", userDict["Name"].GetString());
            Assert.Equal("test@example.com", userDict["Email"].GetString());
        }

        [Fact]
        public async Task GetUserById_ReturnsNotFound_WhenNotExists()
        {
            // Arrange
            var db = CreateDb();
            var controller = new UserController(db, CreateTokenService(), NullLogger<UserController>.Instance);
            
            // Act
            var result = await controller.GetUserById(Guid.NewGuid());

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Promote_ChangesRole_WhenUserExists()
        {
            // Arrange
            var db = CreateDb();
            var userId = Guid.NewGuid();
            db.Users.Add(new User { Id = userId, Name = "Test User", Email = "test@example.com", PasswordHash = "hash", Role = "user" });
            await db.SaveChangesAsync();

            var controller = new UserController(db, CreateTokenService(), NullLogger<UserController>.Instance);
            
            // Act
            var result = await controller.Promote(userId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            
            // Verify role was changed
            var user = await db.Users.FindAsync(userId);
            Assert.NotNull(user);
            Assert.Equal("admin", user.Role);
        }

        [Fact]
        public async Task DeleteUser_RemovesUser_WhenExists()
        {
            // Arrange
            var db = CreateDb();
            var userId = Guid.NewGuid();
            db.Users.Add(new User { Id = userId, Name = "Test User", Email = "test@example.com", PasswordHash = "hash", Role = "user" });
            await db.SaveChangesAsync();

            var controller = new UserController(db, CreateTokenService(), NullLogger<UserController>.Instance);
            
            // Act
            var result = await controller.DeleteUser(userId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            
            // Verify user was deleted
            var user = await db.Users.FindAsync(userId);
            Assert.Null(user);
        }

        [Fact]
        public async Task UpdateUser_ChangesData_WhenExists()
        {
            // Arrange
            var db = CreateDb();
            var userId = Guid.NewGuid();
            db.Users.Add(new User { Id = userId, Name = "Old Name", Email = "test@example.com", PasswordHash = "hash", Role = "user" });
            await db.SaveChangesAsync();

            var controller = new UserController(db, CreateTokenService(), NullLogger<UserController>.Instance);
            var request = new UserController.UpdateUserRequest("New Name", "admin");
            
            // Act
            var result = await controller.UpdateUser(userId, request);

            // Assert
            Assert.IsType<NoContentResult>(result);
            
            // Verify user was updated
            var user = await db.Users.FindAsync(userId);
            Assert.NotNull(user);
            Assert.Equal("New Name", user.Name);
            Assert.Equal("admin", user.Role);
        }

        [Fact]
        public async Task GetCurrentUser_ReturnsUser_WhenExists()
        {
            // Arrange
            var db = CreateDb();
            var userId = Guid.NewGuid();
            db.Users.Add(new User { Id = userId, Name = "Test User", Email = "test@example.com", PasswordHash = "hash", Role = "user" });
            await db.SaveChangesAsync();

            var controller = new UserController(db, CreateTokenService(), NullLogger<UserController>.Instance);
            
            // Mock the User.FindFirst method
            var claims = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(
                new System.Security.Claims.Claim[] { new System.Security.Claims.Claim("sub", userId.ToString()) }
            ));
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = claims }
            };
            
            // Act
            var result = await controller.GetCurrentUser();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            // Convertir a JSON y luego a diccionario para acceder a las propiedades
            var json = JsonSerializer.Serialize(okResult.Value);
            var userDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
            
            Assert.NotNull(userDict);
            Assert.Equal("Test User", userDict["Name"].GetString());
            Assert.Equal("test@example.com", userDict["Email"].GetString());
        }
    }
} 