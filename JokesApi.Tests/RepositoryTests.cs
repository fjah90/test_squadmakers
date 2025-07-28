using System;
using System.Linq;
using System.Threading.Tasks;
using JokesApi.Data;
using JokesApi.Domain.Repositories;
using JokesApi.Entities;
using JokesApi.Infrastructure.Repositories;
using JokesApi.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace JokesApi.Tests
{
    public class RepositoryTests
    {
        private AppDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
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
        }

        [Fact]
        public async Task JokeRepository_AddAsync_WithValidJoke_SavesToDatabase()
        {
            // Arrange
            var context = CreateContext();
            var repository = new JokeRepository(context);
            var joke = new Joke { Id = Guid.NewGuid(), Text = "New joke", AuthorId = Guid.NewGuid() };

            // Act
            await repository.AddAsync(joke);
            await context.SaveChangesAsync();

            // Assert
            var savedJoke = await context.Jokes.FindAsync(joke.Id);
            Assert.NotNull(savedJoke);
            Assert.Equal(joke.Text, savedJoke.Text);
        }

        [Fact]
        public async Task JokeRepository_Remove_WithValidJoke_RemovesFromDatabase()
        {
            // Arrange
            var context = CreateContext();
            var repository = new JokeRepository(context);
            var joke = new Joke { Id = Guid.NewGuid(), Text = "Joke to delete", AuthorId = Guid.NewGuid() };
            context.Jokes.Add(joke);
            await context.SaveChangesAsync();

            // Act
            repository.Remove(joke);
            await context.SaveChangesAsync();

            // Assert
            var deletedJoke = await context.Jokes.FindAsync(joke.Id);
            Assert.Null(deletedJoke);
        }

        [Fact]
        public async Task ThemeRepository_Query_ReturnsAllThemes()
        {
            // Arrange
            var context = CreateContext();
            var repository = new ThemeRepository(context);
            var themes = new[]
            {
                new Theme { Id = Guid.NewGuid(), Name = "Theme 1" },
                new Theme { Id = Guid.NewGuid(), Name = "Theme 2" },
                new Theme { Id = Guid.NewGuid(), Name = "Theme 3" }
            };
            context.Themes.AddRange(themes);
            await context.SaveChangesAsync();

            // Act
            var result = repository.Query.ToList();

            // Assert
            Assert.Equal(3, result.Count());
        }

        [Fact]
        public async Task UserRepository_Query_ReturnsAllUsers()
        {
            // Arrange
            var context = CreateContext();
            var repository = new UserRepository(context);
            var users = new[]
            {
                new User { Id = Guid.NewGuid(), Email = "user1@example.com", Name = "User 1", PasswordHash = "hash", Role = "user" },
                new User { Id = Guid.NewGuid(), Email = "user2@example.com", Name = "User 2", PasswordHash = "hash", Role = "user" },
                new User { Id = Guid.NewGuid(), Email = "admin@example.com", Name = "Admin", PasswordHash = "hash", Role = "admin" }
            };
            context.Users.AddRange(users);
            await context.SaveChangesAsync();

            // Act
            var result = repository.Query.ToList();

            // Assert
            Assert.Equal(3, result.Count());
        }

        [Fact]
        public async Task UserRepository_GetByEmail_WithExistingEmail_ReturnsUser()
        {
            // Arrange
            var context = CreateContext();
            var repository = new UserRepository(context);
            var user = new User { Id = Guid.NewGuid(), Email = "test@example.com", Name = "Test User", PasswordHash = "hash", Role = "user" };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            // Act
            var result = repository.Query.FirstOrDefault(u => u.Email == user.Email);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(user.Email, result.Email);
        }

        [Fact]
        public async Task UserRepository_Exists_WithExistingEmail_ReturnsTrue()
        {
            // Arrange
            var context = CreateContext();
            var repository = new UserRepository(context);
            var user = new User { Id = Guid.NewGuid(), Email = "test@example.com", Name = "Test User", PasswordHash = "hash", Role = "user" };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            // Act
            var result = repository.Query.Any(u => u.Email == user.Email);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task UserRepository_Exists_WithNonExistentEmail_ReturnsFalse()
        {
            // Arrange
            var context = CreateContext();
            var repository = new UserRepository(context);
            var nonExistentEmail = "nonexistent@example.com";

            // Act
            var result = repository.Query.Any(u => u.Email == nonExistentEmail);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UnitOfWork_SaveAsync_WithMultipleChanges_SavesAllChanges()
        {
            // Arrange
            var context = CreateContext();
            var unitOfWork = new UnitOfWork(context);
            var user = new User { Id = Guid.NewGuid(), Email = "test@example.com", Name = "Test User", PasswordHash = "hash", Role = "user" };
            var theme = new Theme { Id = Guid.NewGuid(), Name = "Test Theme" };
            var joke = new Joke { Id = Guid.NewGuid(), Text = "Test joke", AuthorId = user.Id };

            // Act
            context.Users.Add(user);
            context.Themes.Add(theme);
            context.Jokes.Add(joke);
            await unitOfWork.SaveAsync();

            // Assert
            var savedUser = await context.Users.FindAsync(user.Id);
            var savedTheme = await context.Themes.FindAsync(theme.Id);
            var savedJoke = await context.Jokes.FindAsync(joke.Id);
            
            Assert.NotNull(savedUser);
            Assert.NotNull(savedTheme);
            Assert.NotNull(savedJoke);
        }
    }
} 