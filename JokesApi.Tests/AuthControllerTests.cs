using JokesApi.Controllers;
using JokesApi.Data;
using JokesApi.Entities;
using JokesApi.Services;
using JokesApi.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using System.Threading.Tasks;
using System;

namespace JokesApi.Tests;

public class AuthControllerTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static ITokenService CreateTokenService(AppDbContext db) => new TokenService(db, Options.Create(new JwtSettings
    {
        Key = "UnitTestSecretKeyUnitTestSecretKey123",
        Issuer = "TestIssuer",
        Audience = "TestAudience",
        ExpirationMinutes = 5
    }));

    [Fact]
    public async Task Login_ReturnsToken_WhenCredentialsValid()
    {
        // Arrange
        var db = CreateDb();
        var password = "Pass123!";
        var user = new User { Id = Guid.NewGuid(), Email = "user@test.com", Name = "User", PasswordHash = BCrypt.Net.BCrypt.HashPassword(password), Role = "user" };
        db.Users.Add(user);
        db.SaveChanges();
        var controller = new AuthController(db, CreateTokenService(db), NullLogger<AuthController>.Instance);
        var request = new AuthController.LoginRequest(user.Email, password);
        // Act
        var result = await controller.Login(request) as Microsoft.AspNetCore.Mvc.OkObjectResult;
        // Assert
        Assert.NotNull(result);
        var val = result!.Value!;
        var prop = val.GetType().GetProperty("Token");
        Assert.NotNull(prop);
        Assert.False(string.IsNullOrWhiteSpace((string)prop!.GetValue(val)!));
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenInvalid()
    {
        var db = CreateDb();
        var controller = new AuthController(db, CreateTokenService(db), NullLogger<AuthController>.Instance);
        var result = await controller.Login(new AuthController.LoginRequest("bad@test.com", "wrong"));
        Assert.IsType<Microsoft.AspNetCore.Mvc.UnauthorizedObjectResult>(result);
    }
} 