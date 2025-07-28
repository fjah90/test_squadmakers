using System;
using System.Threading.Tasks;
using JokesApi.Application.Ports;
using JokesApi.Application.UseCases;
using JokesApi.Data;
using JokesApi.Entities;
using JokesApi.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace JokesApi.Tests;

public class CombinedJokeTests
{
    [Fact]
    public async Task ExecuteAsync_Returns_Combined_String_With_All_Sources()
    {
        // Arrange mocks for external APIs
        var chuckMock = new Mock<IChuckClient>();
        chuckMock.Setup(c => c.GetRandomJokeAsync(default)).ReturnsAsync("Chuck joke.");
        var dadMock = new Mock<IDadClient>();
        dadMock.Setup(c => c.GetRandomJokeAsync(default)).ReturnsAsync("Dad joke.");

        // In-memory DB for local jokes
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);
        db.Jokes.Add(new Joke
        {
            Id = Guid.NewGuid(),
            Text = "Local joke.",
            AuthorId = Guid.NewGuid()
        });
        db.SaveChanges();

        var uow = new UnitOfWork(db);
        var useCase = new GetCombinedJoke(chuckMock.Object, dadMock.Object, uow);

        // Act
        var result = await useCase.ExecuteAsync();

        // Assert
        Assert.Contains("Chuck joke", result);
        Assert.Contains("Dad joke", result);
        Assert.Contains("Local joke", result);
        Assert.EndsWith(".", result);
    }
} 