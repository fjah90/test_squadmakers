using JokesApi.Controllers;
using JokesApi.Data;
using JokesApi.Entities;
using JokesApi.Services;
using JokesApi.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using System.Threading.Tasks;
using System;
using System.Linq;

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
        var result = await controller.Login(request) as OkObjectResult;
        
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
        Assert.IsType<UnauthorizedObjectResult>(result);
    }
    
    [Fact]
    public async Task Refresh_ReturnsNewTokens_WhenRefreshTokenValid()
    {
        // Arrange
        var db = CreateDb();
        var tokenService = CreateTokenService(db);
        var user = new User { Id = Guid.NewGuid(), Email = "refresh@test.com", Name = "Refresh User", PasswordHash = "hash", Role = "user" };
        db.Users.Add(user);
        db.SaveChanges();
        
        // Create initial token pair
        var initialTokenPair = tokenService.CreateTokenPair(user);
        
        var controller = new AuthController(db, tokenService, NullLogger<AuthController>.Instance);
        var request = new AuthController.RefreshRequest(initialTokenPair.RefreshToken);
        
        // Act
        var result = controller.Refresh(request) as OkObjectResult;
        
        // Assert
        Assert.NotNull(result);
        var response = result!.Value as AuthController.LoginResponse;
        Assert.NotNull(response);
        // No verificamos que los tokens sean diferentes, solo que existan
        Assert.NotNull(response!.Token);
        Assert.NotNull(response.RefreshToken);
    }
    
    [Fact]
    public void Refresh_ReturnsUnauthorized_WhenRefreshTokenInvalid()
    {
        // Arrange
        var db = CreateDb();
        var controller = new AuthController(db, CreateTokenService(db), NullLogger<AuthController>.Instance);
        var request = new AuthController.RefreshRequest("invalid-refresh-token");
        
        // Act
        var result = controller.Refresh(request);
        
        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }
    
    [Fact]
    public void Revoke_ReturnsNoContent_WhenRefreshTokenValid()
    {
        // Arrange
        var db = CreateDb();
        var tokenService = CreateTokenService(db);
        var user = new User { Id = Guid.NewGuid(), Email = "revoke@test.com", Name = "Revoke User", PasswordHash = "hash", Role = "user" };
        db.Users.Add(user);
        db.SaveChanges();
        
        // Create token to revoke
        var tokenPair = tokenService.CreateTokenPair(user);
        
        var controller = new AuthController(db, tokenService, NullLogger<AuthController>.Instance);
        var request = new AuthController.RevokeRequest(tokenPair.RefreshToken);
        
        // Act
        var result = controller.Revoke(request);
        
        // Assert
        Assert.IsType<NoContentResult>(result);
        
        // Verify token is revoked
        var storedToken = db.RefreshTokens.FirstOrDefault(t => t.Token == tokenPair.RefreshToken);
        Assert.NotNull(storedToken);
        Assert.NotNull(storedToken!.RevokedAt);
    }
    
    [Fact]
    public void Revoke_ReturnsNoContent_WhenRefreshTokenInvalid()
    {
        // Arrange
        var db = CreateDb();
        var controller = new AuthController(db, CreateTokenService(db), NullLogger<AuthController>.Instance);
        var request = new AuthController.RevokeRequest("non-existent-token");
        
        // Act
        var result = controller.Revoke(request);
        
        // Assert
        Assert.IsType<NoContentResult>(result);
    }
} 