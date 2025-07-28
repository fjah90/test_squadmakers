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

public class UserControllerIntegrationTests : IClassFixture<WebApplicationFactory<JokesApi.Program>>
{
    private readonly WebApplicationFactory<JokesApi.Program> _factory;
    public UserControllerIntegrationTests(WebApplicationFactory<JokesApi.Program> factory)
    {
        _factory = factory;
    }

    private static string Jwt(IServiceProvider sp, string role="admin")
    {
        var s=sp.GetRequiredService<IOptions<JwtSettings>>().Value;
        var key=new SymmetricSecurityKey(Encoding.UTF8.GetBytes(s.Key));
        var creds=new SigningCredentials(key,SecurityAlgorithms.HmacSha256);
        var token=new JwtSecurityToken(s.Issuer,s.Audience,new[]{
            new Claim(JwtRegisteredClaimNames.Sub,Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role,role)
        },expires:DateTime.UtcNow.AddMinutes(5),signingCredentials:creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [Fact]
    public async Task GetUsers_Returns401_WithoutToken()
    {
        var client=_factory.CreateClient();
        var res=await client.GetAsync("/api/users");
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized,res.StatusCode);
    }

    [Fact]
    public async Task GetUsers_Returns200_WithAdminToken()
    {
        var client=_factory.CreateClient();
        var token=Jwt(_factory.Services);
        client.DefaultRequestHeaders.Authorization=new("Bearer",token);
        var res=await client.GetAsync("/api/users");
        res.EnsureSuccessStatusCode();
    }
} 