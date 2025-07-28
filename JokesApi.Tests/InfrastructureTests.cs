using System;
using System.Threading.Tasks;
using System.Linq;
using JokesApi.Data;
using JokesApi.Domain.Repositories;
using JokesApi.Entities;
using JokesApi.Infrastructure;
using JokesApi.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace JokesApi.Tests;

public class InfrastructureTests
{
    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task UnitOfWork_SaveAsync_SavesChanges()
    {
        // Arrange
        var context = CreateContext();
        var unitOfWork = new UnitOfWork(context);
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@test.com",
            Name = "Test User",
            PasswordHash = "hash",
            Role = "user"
        };

        // Act
        context.Users.Add(user);
        await unitOfWork.SaveAsync();

        // Assert
        var savedUser = await context.Users.FindAsync(user.Id);
        Assert.NotNull(savedUser);
        Assert.Equal("test@test.com", savedUser.Email);
    }

    [Fact]
    public async Task UnitOfWork_SaveAsync_PersistsChanges()
    {
        var context=CreateContext();
        var uow=new UnitOfWork(context);
        context.Users.Add(new User{Id=Guid.NewGuid(),Email="unit@test.com",Name="U",PasswordHash="h",Role="user"});
        var affected=await uow.SaveAsync();
        Assert.Equal(1,affected);
    }

    [Fact]
    public async Task JokeRepository_Query_ReturnsAllJokes()
    {
        // Arrange
        var context = CreateContext();
        var repository = new JokeRepository(context);
        var joke1 = new Joke { Id = Guid.NewGuid(), Text = "Joke 1", AuthorId = Guid.NewGuid() };
        var joke2 = new Joke { Id = Guid.NewGuid(), Text = "Joke 2", AuthorId = Guid.NewGuid() };
        
        context.Jokes.AddRange(joke1, joke2);
        await context.SaveChangesAsync();

        // Act
        var jokes = repository.Query.ToList();

        // Assert
        Assert.Equal(2, jokes.Count);
        Assert.Contains(jokes, j => j.Text == "Joke 1");
        Assert.Contains(jokes, j => j.Text == "Joke 2");
    }

    [Fact]
    public async Task JokeRepository_AddAsync_AddsJoke()
    {
        // Arrange
        var context = CreateContext();
        var repository = new JokeRepository(context);
        var joke = new Joke { Id = Guid.NewGuid(), Text = "New Joke", AuthorId = Guid.NewGuid() };

        // Act
        await repository.AddAsync(joke);
        await context.SaveChangesAsync();

        // Assert
        var savedJoke = await context.Jokes.FindAsync(joke.Id);
        Assert.NotNull(savedJoke);
        Assert.Equal("New Joke", savedJoke.Text);
    }

    [Fact]
    public async Task ThemeRepository_Query_ReturnsAllThemes()
    {
        // Arrange
        var context = CreateContext();
        var repository = new ThemeRepository(context);
        var theme1 = new Theme { Id = Guid.NewGuid(), Name = "Theme 1" };
        var theme2 = new Theme { Id = Guid.NewGuid(), Name = "Theme 2" };
        
        context.Themes.AddRange(theme1, theme2);
        await context.SaveChangesAsync();

        // Act
        var themes = repository.Query.ToList();

        // Assert
        Assert.Equal(2, themes.Count);
        Assert.Contains(themes, t => t.Name == "Theme 1");
        Assert.Contains(themes, t => t.Name == "Theme 2");
    }

    [Fact]
    public async Task UserRepository_Query_ReturnsAllUsers()
    {
        // Arrange
        var context = CreateContext();
        var repository = new UserRepository(context);
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@test.com",
            Name = "Test User",
            PasswordHash = "hash",
            Role = "user"
        };
        
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Act
        var users = repository.Query.ToList();

        // Assert
        Assert.Single(users);
        Assert.Equal("Test User", users[0].Name);
    }

    [Fact]
    public async Task UserRepository_AddAsync_AddsUser()
    {
        // Arrange
        var context = CreateContext();
        var repository = new UserRepository(context);
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "new@test.com",
            Name = "New User",
            PasswordHash = "hash",
            Role = "user"
        };

        // Act
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Assert
        var savedUser = await context.Users.FindAsync(user.Id);
        Assert.NotNull(savedUser);
        Assert.Equal("New User", savedUser.Name);
    }
} 