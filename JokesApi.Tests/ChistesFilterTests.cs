using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JokesApi.Controllers;
using JokesApi.Data;
using JokesApi.Entities;
using JokesApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace JokesApi.Tests;

public class ChistesFilterUnitTests
{
    private static (ChistesController Controller, Guid AuthorId, Guid ThemeId) CreateControllerWithData()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);
        var uow = new UnitOfWork(db);

        // Seed data
        var author = new User
        {
            Id = Guid.NewGuid(),
            Email = "autor@example.com",
            Name = "Autor",
            PasswordHash = "hash",
            Role = "user"
        };
        var theme = new Theme { Id = Guid.NewGuid(), Name = "Animales" };
        db.Users.Add(author);
        db.Themes.Add(theme);
        db.Jokes.AddRange(
            new Joke { Id = Guid.NewGuid(), Text = "Un chiste corto", AuthorId = author.Id },
            new Joke { Id = Guid.NewGuid(), Text = "Este es un chiste muy largo con muchas palabras divertidas", AuthorId = author.Id, Themes = new List<Theme>{ theme } }
        );
        db.SaveChanges();

        // Dummies for other use cases (not used in Filter)
        var combined = new JokesApi.Application.UseCases.GetCombinedJoke(null!, null!, uow);
        var random = new JokesApi.Application.UseCases.GetRandomJoke(null!, null!);
        var paired = new JokesApi.Application.UseCases.GetPairedJokes(null!, null!);

        var controller = new ChistesController(uow, NullLogger<ChistesController>.Instance, combined, random, paired);
        return (controller, author.Id, theme.Id);
    }

    [Fact]
    public async Task Filter_ByMinWords_Returns_Long_Joke_Only()
    {
        var (controller, _, _) = CreateControllerWithData();
        var result = await controller.Filter(6, null, null, null) as OkObjectResult;
        Assert.NotNull(result);
        var list = result!.Value as IEnumerable<object>;
        Assert.Single(list!);
    }

    [Fact]
    public async Task Filter_ByContains_ReturnsMatching()
    {
        var (controller, _, _) = CreateControllerWithData();
        var result = await controller.Filter(null, "largo", null, null) as OkObjectResult;
        Assert.NotNull(result);
        var list = result!.Value as IEnumerable<object>;
        Assert.Single(list!);
    }

    [Fact]
    public async Task Filter_ByAuthor_ReturnsAllAuthorJokes()
    {
        var (controller, authorId, _) = CreateControllerWithData();
        var result = await controller.Filter(null, null, authorId, null) as OkObjectResult;
        Assert.NotNull(result);
        var list = result!.Value as IEnumerable<object>;
        Assert.Equal(2, list!.Count());
    }

    [Fact]
    public async Task Filter_ByTheme_ReturnsThemedJokes()
    {
        var (controller, _, themeId) = CreateControllerWithData();
        var result = await controller.Filter(null, null, null, themeId) as OkObjectResult;
        Assert.NotNull(result);
        var list = result!.Value as IEnumerable<object>;
        Assert.Single(list!);
    }
} 