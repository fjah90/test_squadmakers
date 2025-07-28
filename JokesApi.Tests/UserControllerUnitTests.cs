using System;
using System.Threading.Tasks;
using System.Linq;
using JokesApi.Controllers;
using JokesApi.Data;
using JokesApi.Entities;
using JokesApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace JokesApi.Tests;

public class UserControllerUnitTests
{
    private static AppDbContext CreateDb()
    {
        var options=new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new AppDbContext(options);
    }

    private static UserController CreateController(AppDbContext db,Mock<ITokenService>? tokenMock=null)
    {
        tokenMock ??= new Mock<ITokenService>();
        return new UserController(db,tokenMock.Object,new Mock<ILogger<UserController>>().Object);
    }

    [Fact]
    public async Task Register_NewUser_ReturnsCreated()
    {
        var db=CreateDb();
        var token=new Mock<ITokenService>();
        token.Setup(t=>t.CreateTokenPair(It.IsAny<User>())).Returns(new TokenPair("t","r"));
        var ctl=CreateController(db,token);
        var req=new UserController.RegisterRequest("Name","u@test.com","secret",null);
        var result=await ctl.Register(req);
        var created=Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(1,db.Users.Count());
        token.Verify(t=>t.CreateTokenPair(It.IsAny<User>()),Times.Once);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        var db=CreateDb();
        db.Users.Add(new User{Id=Guid.NewGuid(),Email="dup@test.com",Name="n",PasswordHash="p",Role="user"});
        db.SaveChanges();
        var ctl=CreateController(db);
        var req=new UserController.RegisterRequest("n","dup@test.com","pass",null);
        var res=await ctl.Register(req);
        Assert.IsType<ConflictObjectResult>(res);
    }

    [Fact]
    public async Task GetAllUsers_ReturnsOkList()
    {
        var db=CreateDb();
        db.Users.Add(new User{Id=Guid.NewGuid(),Email="a@test.com",Name="A",PasswordHash="h",Role="user"});
        db.Users.Add(new User{Id=Guid.NewGuid(),Email="b@test.com",Name="B",PasswordHash="h",Role="admin"});
        db.SaveChanges();
        var ctl=CreateController(db);
        var res=await ctl.GetAllUsers();
        var ok=Assert.IsType<OkObjectResult>(res);
        var list=Assert.IsAssignableFrom<System.Collections.IEnumerable>(ok.Value);
    }

    [Fact]
    public async Task GetUserById_ValidId_ReturnsOk()
    {
        var db=CreateDb();
        var user=new User{Id=Guid.NewGuid(),Email="v@test.com",Name="V",PasswordHash="h",Role="user"};
        db.Users.Add(user);db.SaveChanges();
        var ctl=CreateController(db);
        var res=await ctl.GetUserById(user.Id);
        var ok=Assert.IsType<OkObjectResult>(res);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task GetUserById_InvalidId_ReturnsNotFound()
    {
        var ctl=CreateController(CreateDb());
        var res=await ctl.GetUserById(Guid.NewGuid());
        Assert.IsType<NotFoundResult>(res);
    }
} 