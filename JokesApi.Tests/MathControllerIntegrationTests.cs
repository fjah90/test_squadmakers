using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using JokesApi.Settings;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace JokesApi.Tests;

public class MathControllerIntegrationTests : IClassFixture<WebApplicationFactory<JokesApi.Program>>
{
    private readonly WebApplicationFactory<JokesApi.Program> _factory;

    public MathControllerIntegrationTests(WebApplicationFactory<JokesApi.Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder => { });
    }

    private static string GenerateToken(IServiceProvider sp, string role = "user")
    {
        var settings = sp.GetRequiredService<IOptions<JwtSettings>>().Value;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var jwt = new JwtSecurityToken(
            settings.Issuer,
            settings.Audience,
            new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, role)
            },
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    [Fact]
    public async Task NextNumber_ShouldReturn401_WhenNoToken()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync("/api/matematicas/siguiente-numero?number=10");
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task NextNumber_ShouldReturn42_WhenNumberIs41_AndTokenValid()
    {
        var client = _factory.CreateClient();
        var token = GenerateToken(_factory.Services);
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var res = await client.GetAsync("/api/matematicas/siguiente-numero?number=41");
        res.EnsureSuccessStatusCode();
        var json = await res.Content.ReadAsStringAsync();
        Assert.Contains("42", json);
    }

    [Fact]
    public async Task Lcm_ShouldReturnBadRequest_WhenNumbersMissing()
    {
        var client = _factory.CreateClient();
        var token = GenerateToken(_factory.Services);
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var res = await client.GetAsync("/api/matematicas/mcm");
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, res.StatusCode);
    }
} 