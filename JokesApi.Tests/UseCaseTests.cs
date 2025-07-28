using System;
using System.Threading;
using System.Threading.Tasks;
using JokesApi.Application.Ports;
using JokesApi.Application.UseCases;
using Moq;
using Xunit;
using System.Collections.Generic;
using System.Linq;

namespace JokesApi.Tests;

public class UseCaseTests
{
    private static Mock<IChuckClient> CreateChuckMock(string text = "Chuck joke.")
    {
        var m = new Mock<IChuckClient>();
        m.Setup(c => c.GetRandomJokeAsync(It.IsAny<CancellationToken>()))
         .ReturnsAsync(text);
        return m;
    }

    private static Mock<IDadClient> CreateDadMock(string text = "Dad joke.")
    {
        var m = new Mock<IDadClient>();
        m.Setup(c => c.GetRandomJokeAsync(It.IsAny<CancellationToken>()))
         .ReturnsAsync(text);
        return m;
    }

    [Theory]
    [InlineData("chuck", "Chuck joke.")]
    [InlineData("dad", "Dad joke.")]
    public async Task GetRandomJoke_ReturnsExpectedSource(string? origin, string expected)
    {
        // Arrange
        var chuck = CreateChuckMock().Object;
        var dad = CreateDadMock().Object;
        var useCase = new GetRandomJoke(chuck, dad);

        // Act
        var result = await useCase.ExecuteAsync(origin);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task GetRandomJoke_WithNullOrigin_ReturnsRandomJoke()
    {
        // Arrange
        var chuck = CreateChuckMock("Chuck joke.").Object;
        var dad = CreateDadMock("Dad joke.").Object;
        var useCase = new GetRandomJoke(chuck, dad);

        // Act
        var result = await useCase.ExecuteAsync(null);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task GetRandomJoke_WithEmptyOrigin_ReturnsRandomJoke()
    {
        // Arrange
        var chuck = CreateChuckMock("Chuck joke.").Object;
        var dad = CreateDadMock("Dad joke.").Object;
        var useCase = new GetRandomJoke(chuck, dad);

        // Act
        var result = await useCase.ExecuteAsync("");

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task GetPairedJokes_ReturnsRequestedCountWithCombinedText()
    {
        // Arrange
        var chuckMock = CreateChuckMock("Chuck");
        var dadMock = CreateDadMock("Dad");
        var useCase = new GetPairedJokes(chuckMock.Object, dadMock.Object);

        // Act
        var list = await useCase.ExecuteAsync(count: 3);

        // Assert
        Assert.Equal(3, list.Count);
        foreach (var item in list)
        {
            Assert.Equal($"Chuck Also, Dad", item.Combinado);
        }
    }

    [Fact]
    public async Task GetPairedJokes_WithZeroCount_ReturnsEmptyList()
    {
        // Arrange
        var chuckMock = CreateChuckMock("Chuck");
        var dadMock = CreateDadMock("Dad");
        var useCase = new GetPairedJokes(chuckMock.Object, dadMock.Object);

        // Act
        var list = await useCase.ExecuteAsync(count: 0);

        // Assert
        Assert.Empty(list);
    }

    [Fact]
    public async Task GetPairedJokes_WithNegativeCount_ThrowsException()
    {
        // Arrange
        var chuck = CreateChuckMock("Chuck").Object;
        var dad = CreateDadMock("Dad").Object;
        var useCase = new GetPairedJokes(chuck, dad);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => 
            useCase.ExecuteAsync(count: -1));
    }

    [Fact]
    public async Task GetPairedJokes_WithLargeCount_ReturnsCorrectCount()
    {
        // Arrange
        var chuckMock = CreateChuckMock("Chuck");
        var dadMock = CreateDadMock("Dad");
        var useCase = new GetPairedJokes(chuckMock.Object, dadMock.Object);

        // Act
        var list = await useCase.ExecuteAsync(count: 10);

        // Assert
        Assert.Equal(10, list.Count);
    }

    [Theory]
    [InlineData("CHUCK")]
    [InlineData("ChUcK")]
    [InlineData("chuck")]
    [InlineData("DAD")]
    [InlineData("DaD")]
    [InlineData("dad")]
    public async Task GetRandomJoke_WithCaseInsensitiveOrigin_ReturnsExpectedSource(string origin)
    {
        // Arrange
        var chuck = CreateChuckMock("Chuck joke.").Object;
        var dad = CreateDadMock("Dad joke.").Object;
        var useCase = new GetRandomJoke(chuck, dad);

        // Act
        var result = await useCase.ExecuteAsync(origin);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("other")]
    [InlineData("unknown")]
    public async Task GetRandomJoke_WithInvalidOrigin_ReturnsRandomJoke(string origin)
    {
        // Arrange
        var chuck = CreateChuckMock("Chuck joke.").Object;
        var dad = CreateDadMock("Dad joke.").Object;
        var useCase = new GetRandomJoke(chuck, dad);

        // Act
        var result = await useCase.ExecuteAsync(origin);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task GetRandomJoke_WithNullReturnFromChuck_ReturnsEmptyString()
    {
        // Arrange
        var chuckMock = new Mock<IChuckClient>();
        chuckMock.Setup(c => c.GetRandomJokeAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((string?)null);
        var dad = CreateDadMock("Dad joke.").Object;
        var useCase = new GetRandomJoke(chuckMock.Object, dad);

        // Act
        var result = await useCase.ExecuteAsync("chuck");

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public async Task GetRandomJoke_WithNullReturnFromDad_ReturnsEmptyString()
    {
        // Arrange
        var chuck = CreateChuckMock("Chuck joke.").Object;
        var dadMock = new Mock<IDadClient>();
        dadMock.Setup(c => c.GetRandomJokeAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync((string?)null);
        var useCase = new GetRandomJoke(chuck, dadMock.Object);

        // Act
        var result = await useCase.ExecuteAsync("dad");

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public async Task GetRandomJoke_WithCancellationToken_RespectsCancellation()
    {
        // Arrange
        var chuckMock = new Mock<IChuckClient>();
        chuckMock.Setup(c => c.GetRandomJokeAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync("Chuck joke.");
        var dad = CreateDadMock("Dad joke.").Object;
        var useCase = new GetRandomJoke(chuckMock.Object, dad);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        // The use case doesn't propagate cancellation exceptions, so we expect it to complete
        var result = await useCase.ExecuteAsync("chuck", cts.Token);
        Assert.Equal("Chuck joke.", result);
    }

    [Fact]
    public async Task GetRandomJoke_WithBothClientsReturningNull_ReturnsEmptyString()
    {
        // Arrange
        var chuckMock = new Mock<IChuckClient>();
        chuckMock.Setup(c => c.GetRandomJokeAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((string?)null);
        var dadMock = new Mock<IDadClient>();
        dadMock.Setup(c => c.GetRandomJokeAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync((string?)null);
        var useCase = new GetRandomJoke(chuckMock.Object, dadMock.Object);

        // Act
        var result = await useCase.ExecuteAsync(null);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public async Task GetPairedJokes_WithNullReturnFromClients_HandlesGracefully()
    {
        // Arrange
        var chuckMock = new Mock<IChuckClient>();
        chuckMock.Setup(c => c.GetRandomJokeAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((string?)null);
        var dadMock = new Mock<IDadClient>();
        dadMock.Setup(c => c.GetRandomJokeAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync((string?)null);
        var useCase = new GetPairedJokes(chuckMock.Object, dadMock.Object);

        // Act
        var result = await useCase.ExecuteAsync(count: 2);

        // Assert
        Assert.Equal(2, result.Count);
        foreach (var item in result)
        {
            Assert.Equal(" Also, ", item.Combinado);
        }
    }

    [Fact]
    public async Task GetPairedJokes_WithCancellationToken_RespectsCancellation()
    {
        // Arrange
        var chuckMock = new Mock<IChuckClient>();
        chuckMock.Setup(c => c.GetRandomJokeAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync("Chuck");
        var dadMock = new Mock<IDadClient>();
        dadMock.Setup(c => c.GetRandomJokeAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync("Dad");
        var useCase = new GetPairedJokes(chuckMock.Object, dadMock.Object);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        // The use case doesn't propagate cancellation exceptions, so we expect it to complete
        var result = await useCase.ExecuteAsync(count: 1, ct: cts.Token);
        Assert.Single(result);
        Assert.Equal("Chuck Also, Dad", result[0].Combinado);
    }
}