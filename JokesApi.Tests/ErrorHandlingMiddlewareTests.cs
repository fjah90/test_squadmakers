using JokesApi.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;
using Xunit;
using System.IO;
using System.Threading.Tasks;
using System;

namespace JokesApi.Tests;

public class ErrorHandlingMiddlewareTests
{
    [Fact]
    public async Task Middleware_TransformsException_To500()
    {
        RequestDelegate next = ctx => throw new Exception("boom");
        var middleware = new ErrorHandlingMiddleware(next, NullLogger<ErrorHandlingMiddleware>.Instance);
        var context = new DefaultHttpContext();
        var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        await middleware.InvokeAsync(context);

        Assert.Equal(500, context.Response.StatusCode);
        responseBody.Seek(0, SeekOrigin.Begin);
        var json = await new StreamReader(responseBody).ReadToEndAsync();
        Assert.Contains("Internal Server Error", json);
    }
} 