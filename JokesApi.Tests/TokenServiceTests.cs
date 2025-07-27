using System;
using JokesApi.Entities;
using JokesApi.Services;
using JokesApi.Settings;
using Microsoft.Extensions.Options;
using Xunit;
using JokesApi.Data;
using Microsoft.EntityFrameworkCore;

namespace JokesApi.Tests;

public class TokenServiceTests
{
    [Fact]
    public void CreateToken_ReturnsString()
    {
        var settings = Options.Create(new JwtSettings
        {
            Key = "UnitTestSecretKeyUnitTestSecretKey123",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpirationMinutes = 5
        });
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString());
        var db = new AppDbContext(optionsBuilder.Options);

        var service = new TokenService(db, settings);
        var user = new User { Id = Guid.NewGuid(), Email = "unit@test.com", Name = "Unit", PasswordHash = string.Empty, Role = "user" };
        var pair = service.CreateTokenPair(user);
        Assert.False(string.IsNullOrWhiteSpace(pair.Token));
        Assert.False(string.IsNullOrWhiteSpace(pair.RefreshToken));
    }
} 