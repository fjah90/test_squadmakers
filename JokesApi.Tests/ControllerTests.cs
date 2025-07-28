using System;
using System.Net;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections;
using JokesApi.Application.Ports;
using JokesApi.Application.UseCases;
using JokesApi.Controllers;
using JokesApi.Data;
using JokesApi.Entities;
using JokesApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using Microsoft.Extensions.Logging;

namespace JokesApi.Tests;

public class ControllerTests
{
    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task ChistesController_GetRandom_ReturnsJoke()
    {
        // Arrange
        var chuckMock = new Mock<IChuckClient>();
        chuckMock.Setup(c => c.GetRandomJokeAsync(default)).ReturnsAsync("Chuck joke");
        var dadMock = new Mock<IDadClient>();
        dadMock.Setup(c => c.GetRandomJokeAsync(default)).ReturnsAsync("Dad joke");
        
        var getRandomUseCase = new GetRandomJoke(chuckMock.Object, dadMock.Object);
        var getPairedUseCase = new GetPairedJokes(chuckMock.Object, dadMock.Object);
        
        var context = CreateContext();
        var unitOfWork = new UnitOfWork(context);
        var getCombinedUseCase = new GetCombinedJoke(chuckMock.Object, dadMock.Object, unitOfWork);
        
        var loggerMock = new Mock<ILogger<ChistesController>>();
        var controller = new ChistesController(unitOfWork, loggerMock.Object, getCombinedUseCase, getRandomUseCase, getPairedUseCase);

        // Act
        var result = await controller.GetRandom("chuck");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        var jokeProperty = response.GetType().GetProperty("joke");
        Assert.NotNull(jokeProperty);
        Assert.Equal("Chuck joke", jokeProperty.GetValue(response));
    }

    [Fact]
    public async Task ChistesController_GetPaired_ReturnsJokes()
    {
        // Arrange
        var chuckMock = new Mock<IChuckClient>();
        chuckMock.Setup(c => c.GetRandomJokeAsync(default)).ReturnsAsync("Chuck joke");
        var dadMock = new Mock<IDadClient>();
        dadMock.Setup(c => c.GetRandomJokeAsync(default)).ReturnsAsync("Dad joke");
        
        var getRandomUseCase = new GetRandomJoke(chuckMock.Object, dadMock.Object);
        var getPairedUseCase = new GetPairedJokes(chuckMock.Object, dadMock.Object);
        
        var context = CreateContext();
        var unitOfWork = new UnitOfWork(context);
        var getCombinedUseCase = new GetCombinedJoke(chuckMock.Object, dadMock.Object, unitOfWork);
        
        var loggerMock = new Mock<ILogger<ChistesController>>();
        var controller = new ChistesController(unitOfWork, loggerMock.Object, getCombinedUseCase, getRandomUseCase, getPairedUseCase);

        // Act
        var result = await controller.GetPaired();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var jokes = okResult.Value;
        Assert.NotNull(jokes);
        // Accept both List<object> and List<PairedJokeResult>
        Assert.True(jokes is IEnumerable);
    }

    [Fact]
    public async Task ChistesController_GetCombined_ReturnsCombinedJoke()
    {
        // Arrange
        var chuckMock = new Mock<IChuckClient>();
        chuckMock.Setup(c => c.GetRandomJokeAsync(default)).ReturnsAsync("Chuck joke");
        var dadMock = new Mock<IDadClient>();
        dadMock.Setup(c => c.GetRandomJokeAsync(default)).ReturnsAsync("Dad joke");
        
        var getRandomUseCase = new GetRandomJoke(chuckMock.Object, dadMock.Object);
        var getPairedUseCase = new GetPairedJokes(chuckMock.Object, dadMock.Object);
        
        var context = CreateContext();
        var unitOfWork = new UnitOfWork(context);
        var getCombinedUseCase = new GetCombinedJoke(chuckMock.Object, dadMock.Object, unitOfWork);
        
        var loggerMock = new Mock<ILogger<ChistesController>>();
        var controller = new ChistesController(unitOfWork, loggerMock.Object, getCombinedUseCase, getRandomUseCase, getPairedUseCase);

        // Act
        var result = await controller.GetCombined();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        var combinedProperty = response.GetType().GetProperty("combined");
        Assert.NotNull(combinedProperty);
        var combinedValue = combinedProperty.GetValue(response).ToString();
        Assert.Contains("Chuck joke", combinedValue);
        Assert.Contains("Dad joke", combinedValue);
    }

    [Fact]
    public void MathController_Lcm_ReturnsCorrectResult()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<MathController>>();
        var controller = new MathController(loggerMock.Object);

        // Act
        var result = controller.Lcm("12,18");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        var lcmProperty = response.GetType().GetProperty("lcm");
        Assert.NotNull(lcmProperty);
        var lcmValue = lcmProperty.GetValue(response);
        Assert.Equal(36, Convert.ToInt32(lcmValue));
    }

    [Fact]
    public void MathController_Lcm_WithInvalidInput_ReturnsBadRequest()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<MathController>>();
        var controller = new MathController(loggerMock.Object);

        // Act
        var result = controller.Lcm("invalid");

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public void MathController_Add_ReturnsSum()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<MathController>>();
        var controller = new MathController(loggerMock.Object);

        // Act
        var result = controller.Add(5, 3);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(8, okResult.Value);
    }

    [Fact]
    public void MathController_Subtract_ReturnsDifference()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<MathController>>();
        var controller = new MathController(loggerMock.Object);

        // Act
        var result = controller.Subtract(10, 4);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(6, okResult.Value);
    }

    [Fact]
    public void MathController_Multiply_ReturnsProduct()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<MathController>>();
        var controller = new MathController(loggerMock.Object);

        // Act
        var result = controller.Multiply(7, 6);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(42, okResult.Value);
    }
} 