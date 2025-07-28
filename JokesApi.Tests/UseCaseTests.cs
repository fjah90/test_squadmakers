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
}