using JokesApi.Controllers;
using JokesApi.Entities;
using JokesApi.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using System.Text.Json;
using JokesApi.Domain.Repositories;
using System.Net;
using System.Threading.Tasks;
using System.Net.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using JokesApi.Application.UseCases;
using JokesApi.Application.Ports;

namespace JokesApi.Tests;

public class ChistesControllerTests
{
    private static IHttpClientFactory CreateFactory()
    {
        var mock = new Mock<IHttpClientFactory>();
        mock.Setup(f => f.CreateClient("Chuck")).Returns(CreateClient("{\"value\":\"Chuck!\"}"));
        mock.Setup(f => f.CreateClient("Dad")).Returns(CreateClient("{\"joke\":\"Dad!\"}"));
        return mock.Object;
    }

    private static HttpClient CreateClient(string json)
    {
        var handler = new FakeHttpHandler(_ => {
            var r = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) };
            r.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            return r;
        });
        return new HttpClient(handler)
        {
            BaseAddress = new Uri("https://fake.local")
        };
    }

    private static IChuckClient CreateChuckClient()
    {
        var mock = new Mock<IChuckClient>();
        mock.Setup(c => c.GetRandomJokeAsync(It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync("Chuck joke!");
        return mock.Object;
    }

    private static IDadClient CreateDadClient()
    {
        var mock = new Mock<IDadClient>();
        mock.Setup(c => c.GetRandomJokeAsync(It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync("Dad joke!");
        return mock.Object;
    }

    [Fact]
    public async Task GetPaired_ReturnsFiveItems()
    {
        // Arrange
        var uow = new Mock<IUnitOfWork>();
        var chuckClient = CreateChuckClient();
        var dadClient = CreateDadClient();
        var randomUseCase = new GetRandomJoke(chuckClient, dadClient);
        var pairedUseCase = new GetPairedJokes(chuckClient, dadClient);
        var combinedUseCase = new GetCombinedJoke(chuckClient, dadClient, uow.Object);
        
        var controller = new ChistesController(
            uow.Object, 
            NullLogger<ChistesController>.Instance, 
            combinedUseCase,
            randomUseCase,
            pairedUseCase);

        // Act
        var res = await controller.GetPaired() as OkObjectResult;

        // Assert
        Assert.NotNull(res);
        var list = res!.Value as List<GetPairedJokes.PairedJokeResult>;
        Assert.Equal(5, list!.Count);
    }

    [Fact]
    public async Task GetCombined_ReturnsCombinedString()
    {
        var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<JokesApi.Data.AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new JokesApi.Data.AppDbContext(options);
        db.Jokes.Add(new Joke { Id = Guid.NewGuid(), Text = "Local joke", AuthorId = Guid.NewGuid() });
        db.SaveChanges();
        var jokeRepo = new JokesApi.Infrastructure.Repositories.JokeRepository(db);
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(x => x.Jokes).Returns(jokeRepo);
        
        var chuckClient = CreateChuckClient();
        var dadClient = CreateDadClient();
        var randomUseCase = new GetRandomJoke(chuckClient, dadClient);
        var pairedUseCase = new GetPairedJokes(chuckClient, dadClient);
        var combinedUseCase = new GetCombinedJoke(chuckClient, dadClient, uow.Object);
        
        var controller = new ChistesController(
            uow.Object, 
            NullLogger<ChistesController>.Instance, 
            combinedUseCase,
            randomUseCase,
            pairedUseCase);

        var res = await controller.GetCombined() as OkObjectResult;
        Assert.NotNull(res);
    }
    
    [Fact]
    public async Task GetRandom_ReturnsJoke()
    {
        // Arrange
        var uow = new Mock<IUnitOfWork>();
        var chuckClient = CreateChuckClient();
        var dadClient = CreateDadClient();
        var randomUseCase = new GetRandomJoke(chuckClient, dadClient);
        var pairedUseCase = new GetPairedJokes(chuckClient, dadClient);
        var combinedUseCase = new GetCombinedJoke(chuckClient, dadClient, uow.Object);
        
        var controller = new ChistesController(
            uow.Object, 
            NullLogger<ChistesController>.Instance, 
            combinedUseCase,
            randomUseCase,
            pairedUseCase);

        // Act
        var res = await controller.GetRandom("chuck") as OkObjectResult;

        // Assert
        Assert.NotNull(res);
        // Simplemente verificamos que la respuesta no sea nula
        Assert.NotNull(res.Value);
    }
} 