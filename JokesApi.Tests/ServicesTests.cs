using JokesApi.Data;
using JokesApi.Entities;
using JokesApi.Services;
using JokesApi.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using Microsoft.IdentityModel.Tokens;

namespace JokesApi.Tests;

public class ServicesTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public void TokenService_CreateTokenPair_ReturnsValidTokenPair()
    {
        // Arrange
        var db = CreateDb();
        var tokenService = new TokenService(db, Options.Create(new JwtSettings
        {
            Key = "UnitTestSecretKeyUnitTestSecretKey123",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpirationMinutes = 5
        }));
        var user = new User { Id = Guid.NewGuid(), Name = "Test User", Email = "test@example.com", Role = "user", PasswordHash = "hash" };

        // Act
        var tokenPair = tokenService.CreateTokenPair(user);

        // Assert
        Assert.NotNull(tokenPair);
        Assert.NotNull(tokenPair.Token);
        Assert.NotNull(tokenPair.RefreshToken);
        Assert.NotEmpty(tokenPair.Token);
        Assert.NotEmpty(tokenPair.RefreshToken);
    }

    [Fact]
    public void TokenService_Refresh_ReturnsNewTokenPair()
    {
        // Arrange
        var db = CreateDb();
        var tokenService = new TokenService(db, Options.Create(new JwtSettings
        {
            Key = "UnitTestSecretKeyUnitTestSecretKey123",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpirationMinutes = 5
        }));
        var user = new User { Id = Guid.NewGuid(), Name = "Test User", Email = "test@example.com", Role = "user", PasswordHash = "hash" };
        
        // Add user to database first
        db.Users.Add(user);
        db.SaveChanges();
        
        // Create initial token pair and save to database
        var initialTokenPair = tokenService.CreateTokenPair(user);

        // Act
        var newTokenPair = tokenService.Refresh(initialTokenPair.RefreshToken);

        // Assert
        Assert.NotNull(newTokenPair);
        Assert.NotNull(newTokenPair.Token);
        Assert.NotNull(newTokenPair.RefreshToken);
        Assert.NotEqual(initialTokenPair.RefreshToken, newTokenPair.RefreshToken);
    }

    [Fact]
    public void TokenService_Refresh_WithInvalidToken_ThrowsException()
    {
        // Arrange
        var db = CreateDb();
        var tokenService = new TokenService(db, Options.Create(new JwtSettings
        {
            Key = "UnitTestSecretKeyUnitTestSecretKey123",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpirationMinutes = 5
        }));

        // Act & Assert
        Assert.Throws<SecurityTokenException>(() => tokenService.Refresh("invalid-token"));
    }

    [Fact]
    public void TokenService_Revoke_WithValidToken_RevokesToken()
    {
        // Arrange
        var db = CreateDb();
        var tokenService = new TokenService(db, Options.Create(new JwtSettings
        {
            Key = "UnitTestSecretKeyUnitTestSecretKey123",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpirationMinutes = 5
        }));
        var user = new User { Id = Guid.NewGuid(), Name = "Test User", Email = "test@example.com", Role = "user", PasswordHash = "hash" };
        var tokenPair = tokenService.CreateTokenPair(user);

        // Act
        tokenService.Revoke(tokenPair.RefreshToken);

        // Assert
        var storedToken = db.RefreshTokens.FirstOrDefault(t => t.Token == tokenPair.RefreshToken);
        Assert.NotNull(storedToken);
        Assert.NotNull(storedToken.RevokedAt);
    }

    [Fact]
    public void TokenService_Revoke_WithInvalidToken_DoesNothing()
    {
        // Arrange
        var db = CreateDb();
        var tokenService = new TokenService(db, Options.Create(new JwtSettings
        {
            Key = "UnitTestSecretKeyUnitTestSecretKey123",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpirationMinutes = 5
        }));

        // Act & Assert - Should not throw
        tokenService.Revoke("invalid-token");
    }

    [Fact]
    public void TokenService_Constructor_InitializesCorrectly()
    {
        // Arrange
        var db = CreateDb();
        var settings = Options.Create(new JwtSettings
        {
            Key = "UnitTestSecretKeyUnitTestSecretKey123",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpirationMinutes = 5
        });

        // Act
        var tokenService = new TokenService(db, settings);

        // Assert
        Assert.NotNull(tokenService);
    }
} 