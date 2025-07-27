using System;
using JokesApi.Entities;
using JokesApi.Services;
using JokesApi.Settings;
using Microsoft.Extensions.Options;
using Xunit;

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
        var service = new TokenService(settings);
        var user = new User { Id = Guid.NewGuid(), Email = "unit@test.com", Name = "Unit", PasswordHash = string.Empty, Role = "user" };
        var token = service.CreateToken(user);
        Assert.False(string.IsNullOrWhiteSpace(token));
    }
} 