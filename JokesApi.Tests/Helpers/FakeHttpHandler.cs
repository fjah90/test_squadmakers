using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace JokesApi.Tests.Helpers;

public class FakeHttpHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _res;
    public FakeHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> res) => _res = res;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(_res(request));
    }
} 