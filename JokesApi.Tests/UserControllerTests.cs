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
using Microsoft.AspNetCore.Http;

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
                .Returns(new TokenPair("jwt-token", "refresh-token"));
            return mockTokenService.Object;
        }

        [Fact]
        public async Task Register_ReturnsCreated_WithValidData()
        {
            // Arrange
            var db = CreateDb();
            var controller = new UserController(db, CreateTokenService(), NullLogger<UserController>.Instance);
            var request = new UserController.RegisterRequest("Test User", "test@example.com", "password", null);
            
            // Act
            var result = await controller.Register(request);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(201, createdResult.StatusCode);
            
            // Verify user was created in database
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == "test@example.com");
            Assert.NotNull(user);
            Assert.Equal("Test User", user.Name);
            Assert.Equal("user", user.Role);
        }

        [Fact]
        public async Task Register_ReturnsConflict_WhenEmailExists()
        {
            // Arrange
            var db = CreateDb();
            db.Users.Add(new User { 
                Id = Guid.NewGuid(), 
                Name = "Existing User", 
                Email = "existing@example.com", 
                PasswordHash = "hash",
                Role = "user"
            });
            await db.SaveChangesAsync();

            var controller = new UserController(db, CreateTokenService(), NullLogger<UserController>.Instance);
            var request = new UserController.RegisterRequest("New User", "existing@example.com", "password", null);
            
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
            var request = new UserController.RegisterRequest("Admin User", "admin@example.com", "password", "admin");
            
            // Act
            var result = await controller.Register(request);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            
            // Verify user was created with admin role
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == "admin@example.com");
            Assert.NotNull(user);
            Assert.Equal("admin", user.Role);
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
            var okResult = Assert.IsType<OkObjectResult>(result);
            
            // Verify user was updated
            var user = await db.Users.FindAsync(userId);
            Assert.NotNull(user);
            Assert.Equal("New Name", user.Name);
            Assert.Equal("admin", user.Role);
        }

        /*
        [Fact]
        public async Task GetCurrentUser_ReturnsUser_WhenExists()
        {
            // Arrange
            var db = CreateDb();
            var userId = Guid.NewGuid();
            var user = new User { Id = userId, Name = "Test User", Email = "test@example.com", Role = "user" };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var controller = new UserController(db, CreateTokenService(), NullLogger<UserController>.Instance);
            
            // Setup user claims
            var claims = new List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim("sub", userId.ToString())
            };
            var identity = new System.Security.Claims.ClaimsIdentity(claims);
            var principal = new System.Security.Claims.ClaimsPrincipal(identity);
            
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = principal
                }
            };

            // Act
            var result = await controller.GetCurrentUser();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedUser = Assert.IsType<User>(okResult.Value);
            Assert.Equal(userId, returnedUser.Id);
        }

        /*
        [Fact]
        public async Task GetCurrentUser_ReturnsUnauthorized_WhenNoUserClaim()
        {
            // Arrange
            var db = CreateDb();
            var controller = new UserController(db, CreateTokenService(), NullLogger<UserController>.Instance);

            // Act
            var result = await controller.GetCurrentUser();

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task GetCurrentUser_ReturnsUnauthorized_WhenInvalidUserId()
        {
            // Arrange
            var db = CreateDb();
            var controller = new UserController(db, CreateTokenService(), NullLogger<UserController>.Instance);
            
            // Setup user claims with invalid GUID
            var claims = new List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim("sub", "invalid-guid")
            };
            var identity = new System.Security.Claims.ClaimsIdentity(claims);
            var principal = new System.Security.Claims.ClaimsPrincipal(identity);
            
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = principal
                }
            };

            // Act
            var result = await controller.GetCurrentUser();

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task GetCurrentUser_ReturnsNotFound_WhenUserNotExists()
        {
            // Arrange
            var db = CreateDb();
            var userId = Guid.NewGuid();
            var controller = new UserController(db, CreateTokenService(), NullLogger<UserController>.Instance);
            
            // Setup user claims
            var claims = new List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim("sub", userId.ToString())
            };
            var identity = new System.Security.Claims.ClaimsIdentity(claims);
            var principal = new System.Security.Claims.ClaimsPrincipal(identity);
            
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = principal
                }
            };

            // Act
            var result = await controller.GetCurrentUser();

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Register_WithNullName_ReturnsBadRequest()
        {
            // Arrange
            var db = CreateDb();
            var controller = new UserController(db, CreateTokenService(), NullLogger<UserController>.Instance);
            var request = new UserController.RegisterRequest(null!, "test@example.com", "password", null);
            
            // Act
            var result = await controller.Register(request);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Register_WithEmptyName_ReturnsBadRequest()
        {
            // Arrange
            var db = CreateDb();
            var controller = new UserController(db, CreateTokenService(), NullLogger<UserController>.Instance);
            var request = new UserController.RegisterRequest("", "test@example.com", "password", null);
            
            // Act
            var result = await controller.Register(request);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Register_WithNullEmail_ReturnsBadRequest()
        {
            // Arrange
            var db = CreateDb();
            var controller = new UserController(db, CreateTokenService(), NullLogger<UserController>.Instance);
            var request = new UserController.RegisterRequest("Test User", null!, "password", null);
            
            // Act
            var result = await controller.Register(request);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Register_WithEmptyEmail_ReturnsBadRequest()
        {
            // Arrange
            var db = CreateDb();
            var controller = new UserController(db, CreateTokenService(), NullLogger<UserController>.Instance);
            var request = new UserController.RegisterRequest("Test User", "", "password", null);
            
            // Act
            var result = await controller.Register(request);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Register_WithNullPassword_ReturnsBadRequest()
        {
            // Arrange
            var db = CreateDb();
            var controller = new UserController(db, CreateTokenService(), NullLogger<UserController>.Instance);
            var request = new UserController.RegisterRequest("Test User", "test@example.com", null!, null);
            
            // Act
            var result = await controller.Register(request);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Register_WithEmptyPassword_ReturnsBadRequest()
        {
            // Arrange
            var db = CreateDb();
            var controller = new UserController(db, CreateTokenService(), NullLogger<UserController>.Instance);
            var request = new UserController.RegisterRequest("Test User", "test@example.com", "", null);
            
            // Act
            var result = await controller.Register(request);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Register_WithInvalidEmailFormat_ReturnsBadRequest()
        {
            // Arrange
            var db = CreateDb();
            var controller = new UserController(db, CreateTokenService(), NullLogger<UserController>.Instance);
            var request = new UserController.RegisterRequest("Test User", "invalid-email", "password", null);
            
            // Act
            var result = await controller.Register(request);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Register_WithWeakPassword_ReturnsBadRequest()
        {
            // Arrange
            var db = CreateDb();
            var controller = new UserController(db, CreateTokenService(), NullLogger<UserController>.Instance);
            var request = new UserController.RegisterRequest("Test User", "test@example.com", "123", null);
            
            // Act
            var result = await controller.Register(request);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Register_WithDatabaseException_Returns500()
        {
            // Arrange
            var db = CreateDb();
            var controller = new UserController(db, CreateTokenService(), NullLogger<UserController>.Instance);
            var request = new UserController.RegisterRequest("Test User", "test@example.com", "password", null);
            
            // Simulate database exception by disposing the context
            db.Dispose();
            
            // Act
            var result = await controller.Register(request);

            // Assert
            Assert.IsType<ObjectResult>(result);
            var objectResult = (ObjectResult)result;
            Assert.Equal(500, objectResult.StatusCode);
        }

        [Fact]
        public async Task GetAllUsers_ReturnsForbidden_WhenNotAdmin()
        {
            // Arrange
            var db = CreateDb();
            var controller = new UserController(db, CreateTokenService(), NullLogger<UserController>.Instance);
            
            // Setup user claims for non-admin user
            var claims = new List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim("sub", Guid.NewGuid().ToString()),
                new System.Security.Claims.Claim("role", "user")
            };
            var identity = new System.Security.Claims.ClaimsIdentity(claims);
            var principal = new System.Security.Claims.ClaimsPrincipal(identity);
            
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = principal
                }
            };

            // Act
            var result = await controller.GetAllUsers();

            // Assert
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task Promote_ReturnsForbidden_WhenNotAdmin()
        {
            // Arrange
            var db = CreateDb();
            var controller = new UserController(db, CreateTokenService(), NullLogger<UserController>.Instance);
            
            // Setup user claims for non-admin user
            var claims = new List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim("sub", Guid.NewGuid().ToString()),
                new System.Security.Claims.Claim("role", "user")
            };
            var identity = new System.Security.Claims.ClaimsIdentity(claims);
            var principal = new System.Security.Claims.ClaimsPrincipal(identity);
            
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = principal
                }
            };

            // Act
            var result = await controller.Promote(Guid.NewGuid());

            // Assert
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task DeleteUser_ReturnsForbidden_WhenNotAdmin()
        {
            // Arrange
            var db = CreateDb();
            var controller = new UserController(db, CreateTokenService(), NullLogger<UserController>.Instance);
            
            // Setup user claims for non-admin user
            var claims = new List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim("sub", Guid.NewGuid().ToString()),
                new System.Security.Claims.Claim("role", "user")
            };
            var identity = new System.Security.Claims.ClaimsIdentity(claims);
            var principal = new System.Security.Claims.ClaimsPrincipal(identity);
            
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = principal
                }
            };

            // Act
            var result = await controller.DeleteUser(Guid.NewGuid());

            // Assert
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task UpdateUser_ReturnsForbidden_WhenNotAdmin()
        {
            // Arrange
            var db = CreateDb();
            var controller = new UserController(db, CreateTokenService(), NullLogger<UserController>.Instance);
            
            // Setup user claims for non-admin user
            var claims = new List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim("sub", Guid.NewGuid().ToString()),
                new System.Security.Claims.Claim("role", "user")
            };
            var identity = new System.Security.Claims.ClaimsIdentity(claims);
            var principal = new System.Security.Claims.ClaimsPrincipal(identity);
            
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = principal
                }
            };

            var request = new UserController.UpdateUserRequest("New Name", "newemail@example.com");

            // Act
            var result = await controller.UpdateUser(Guid.NewGuid(), request);

            // Assert
            Assert.IsType<ForbidResult>(result);
        }
        */
    }
} 