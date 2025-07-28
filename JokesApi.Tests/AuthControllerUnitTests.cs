using System;
using System.Threading.Tasks;
using JokesApi.Controllers;
using JokesApi.Data;
using JokesApi.Entities;
using JokesApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Microsoft.IdentityModel.Tokens;

namespace JokesApi.Tests;

public class AuthControllerUnitTests
{
    private static AppDbContext CreateDb()
    {
        var options=new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new AppDbContext(options);
    }

    private static User AddUser(AppDbContext db,string email="user@test.com",string password="pass")
    {
        var user=new User{Id=Guid.NewGuid(),Email=email,Name="User",PasswordHash=BCrypt.Net.BCrypt.HashPassword(password),Role="user"};
        db.Users.Add(user);db.SaveChanges();
        return user;
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOk()
    {
        var db=CreateDb();
        var user=AddUser(db);
        var tokenService=new Mock<ITokenService>();
        tokenService.Setup(t=>t.CreateTokenPair(user)).Returns(new TokenPair("token","refresh"));
        var controller=new AuthController(db,tokenService.Object,new Mock<ILogger<AuthController>>().Object);
        var result=await controller.Login(new AuthController.LoginRequest(user.Email,"pass"));
        var ok=Assert.IsType<OkObjectResult>(result);
        var resp=Assert.IsType<AuthController.LoginResponse>(ok.Value);
        Assert.Equal("token",resp.Token);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        var db=CreateDb();
        var user=AddUser(db);
        var tokenService=new Mock<ITokenService>();
        var controller=new AuthController(db,tokenService.Object,new Mock<ILogger<AuthController>>().Object);
        var result=await controller.Login(new AuthController.LoginRequest(user.Email,"wrong"));
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public void Refresh_WithValidToken_ReturnsOk()
    {
        var db=CreateDb();
        var tokenService=new Mock<ITokenService>();
        tokenService.Setup(t=>t.Refresh("good")).Returns(new TokenPair("tok","newref"));
        var controller=new AuthController(db,tokenService.Object,new Mock<ILogger<AuthController>>().Object);
        var result=controller.Refresh(new AuthController.RefreshRequest("good"));
        var ok=Assert.IsType<OkObjectResult>(result);
        var resp=Assert.IsType<AuthController.LoginResponse>(ok.Value);
        Assert.Equal("tok",resp.Token);
    }

    [Fact]
    public void Refresh_WithInvalidToken_ReturnsUnauthorized()
    {
        var db=CreateDb();
        var tokenService=new Mock<ITokenService>();
        tokenService.Setup(t=>t.Refresh("bad")).Throws(new SecurityTokenException("bad"));
        var controller=new AuthController(db,tokenService.Object,new Mock<ILogger<AuthController>>().Object);
        var result=controller.Refresh(new AuthController.RefreshRequest("bad"));
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public void Revoke_AlwaysReturnsNoContent()
    {
        var db=CreateDb();
        var tokenService=new Mock<ITokenService>();
        var controller=new AuthController(db,tokenService.Object,new Mock<ILogger<AuthController>>().Object);
        var res1=controller.Revoke(null);
        Assert.IsType<NoContentResult>(res1);
        var res2=controller.Revoke(new AuthController.RevokeRequest("any"));
        Assert.IsType<NoContentResult>(res2);
        tokenService.Verify(t=>t.Revoke("any"),Times.Once);
    }
} 