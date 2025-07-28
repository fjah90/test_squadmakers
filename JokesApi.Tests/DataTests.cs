using System;
using System.Linq;
using JokesApi.Data;
using JokesApi.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;
using System.Threading.Tasks;

namespace JokesApi.Tests;

public class DataTests
{
    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public void AppDbContext_CanCreateInstance()
    {
        // Arrange & Act
        var context = CreateContext();

        // Assert
        Assert.NotNull(context);
        Assert.NotNull(context.Jokes);
        Assert.NotNull(context.Users);
        Assert.NotNull(context.Themes);
        Assert.NotNull(context.RefreshTokens);
    }

    [Fact]
    public async Task AppDbContext_CanAddJoke()
    {
        // Arrange
        var context = CreateContext();
        var joke = new Joke
        {
            Id = Guid.NewGuid(),
            Text = "Test joke",
            AuthorId = Guid.NewGuid(),
            Source = "Local"
        };

        // Act
        context.Jokes.Add(joke);
        await context.SaveChangesAsync();

        // Assert
        var savedJoke = await context.Jokes.FindAsync(joke.Id);
        Assert.NotNull(savedJoke);
        Assert.Equal("Test joke", savedJoke.Text);
    }

    [Fact]
    public async Task AppDbContext_CanAddUser()
    {
        // Arrange
        var context = CreateContext();
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
        await context.SaveChangesAsync();

        // Assert
        var savedUser = await context.Users.FindAsync(user.Id);
        Assert.NotNull(savedUser);
        Assert.Equal("test@test.com", savedUser.Email);
    }

    [Fact]
    public async Task AppDbContext_CanAddTheme()
    {
        // Arrange
        var context = CreateContext();
        var theme = new Theme
        {
            Id = Guid.NewGuid(),
            Name = "Test Theme"
        };

        // Act
        context.Themes.Add(theme);
        await context.SaveChangesAsync();

        // Assert
        var savedTheme = await context.Themes.FindAsync(theme.Id);
        Assert.NotNull(savedTheme);
        Assert.Equal("Test Theme", savedTheme.Name);
    }

    [Fact]
    public async Task AppDbContext_CanAddRefreshToken()
    {
        // Arrange
        var context = CreateContext();
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = "test-token",
            UserId = Guid.NewGuid(),
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        // Act
        context.RefreshTokens.Add(refreshToken);
        await context.SaveChangesAsync();

        // Assert
        var savedToken = await context.RefreshTokens.FindAsync(refreshToken.Id);
        Assert.NotNull(savedToken);
        Assert.Equal("test-token", savedToken.Token);
    }

    [Fact]
    public async Task AppDbContext_CanQueryJokes()
    {
        // Arrange
        var context = CreateContext();
        var joke1 = new Joke { Id = Guid.NewGuid(), Text = "Joke 1", AuthorId = Guid.NewGuid() };
        var joke2 = new Joke { Id = Guid.NewGuid(), Text = "Joke 2", AuthorId = Guid.NewGuid() };
        
        context.Jokes.AddRange(joke1, joke2);
        await context.SaveChangesAsync();

        // Act
        var jokes = await context.Jokes.ToListAsync();

        // Assert
        Assert.Equal(2, jokes.Count);
        Assert.Contains(jokes, j => j.Text == "Joke 1");
        Assert.Contains(jokes, j => j.Text == "Joke 2");
    }

    [Fact]
    public async Task AppDbContext_CanQueryUsers()
    {
        // Arrange
        var context = CreateContext();
        var user1 = new User { Id = Guid.NewGuid(), Email = "user1@test.com", Name = "User 1", PasswordHash = "hash", Role = "user" };
        var user2 = new User { Id = Guid.NewGuid(), Email = "user2@test.com", Name = "User 2", PasswordHash = "hash", Role = "admin" };
        
        context.Users.AddRange(user1, user2);
        await context.SaveChangesAsync();

        // Act
        var users = await context.Users.ToListAsync();

        // Assert
        Assert.Equal(2, users.Count);
        Assert.Contains(users, u => u.Email == "user1@test.com");
        Assert.Contains(users, u => u.Email == "user2@test.com");
    }

    [Fact]
    public async Task AppDbContext_CanQueryThemes()
    {
        // Arrange
        var context = CreateContext();
        var theme1 = new Theme { Id = Guid.NewGuid(), Name = "Theme 1" };
        var theme2 = new Theme { Id = Guid.NewGuid(), Name = "Theme 2" };
        
        context.Themes.AddRange(theme1, theme2);
        await context.SaveChangesAsync();

        // Act
        var themes = await context.Themes.ToListAsync();

        // Assert
        Assert.Equal(2, themes.Count);
        Assert.Contains(themes, t => t.Name == "Theme 1");
        Assert.Contains(themes, t => t.Name == "Theme 2");
    }
} 