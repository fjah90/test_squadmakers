using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using JokesApi.Infrastructure.External;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace JokesApi.Tests;

public class ExternalClientsRealTests
{
    private class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;
        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)=>_handler=handler;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            =>Task.FromResult(_handler(request));
    }

    private static IHttpClientFactory CreateFactory(string baseAddress,string json)
    {
        var handler=new StubHandler(_=> new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(json)});
        var client=new HttpClient(handler){BaseAddress=new Uri(baseAddress)};
        var factory=new Mock<IHttpClientFactory>();
        factory.Setup(f=>f.CreateClient(It.IsAny<string>())).Returns(client);
        return factory.Object;
    }

    [Fact]
    public async Task ChuckClient_ReturnsValue()
    {
        // Arrange
        var factory=CreateFactory("https://api.chuck", "{\"value\":\"Chuck quote\"}");
        var client=new ChuckClient(factory);
        // Act
        var joke=await client.GetRandomJokeAsync();
        // Assert
        Assert.Equal("Chuck quote", joke);
    }

    [Fact]
    public async Task DadClient_ReturnsValue()
    {
        var factory=CreateFactory("https://icanhazdad", "{\"joke\":\"Dad joke\"}");
        var client=new DadClient(factory);
        var joke=await client.GetRandomJokeAsync();
        Assert.Equal("Dad joke", joke);
    }
} 