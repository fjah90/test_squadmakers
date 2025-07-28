using JokesApi.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using System;
using System.IO;
using System.Threading.Tasks;

namespace JokesApi.Tests;

public class MiddlewareTests
{
    [Fact]
    public async Task ErrorHandlingMiddleware_HandlesException()
    {
        // Arrange
        var logger = new Mock<ILogger<ErrorHandlingMiddleware>>();
        var middleware = new ErrorHandlingMiddleware(
            next: (context) => throw new Exception("Test exception"),
            logger: logger.Object
        );

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(500, context.Response.StatusCode);
    }

    [Fact]
    public async Task ErrorHandlingMiddleware_HandlesSpecificException()
    {
        // Arrange
        var logger = new Mock<ILogger<ErrorHandlingMiddleware>>();
        var middleware = new ErrorHandlingMiddleware(
            next: (context) => throw new InvalidOperationException("Invalid operation"),
            logger: logger.Object
        );

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(500, context.Response.StatusCode);
    }

    [Fact]
    public async Task ErrorHandlingMiddleware_HandlesUnauthorizedException()
    {
        // Arrange
        var logger = new Mock<ILogger<ErrorHandlingMiddleware>>();
        var middleware = new ErrorHandlingMiddleware(
            next: (context) => throw new UnauthorizedAccessException("Unauthorized"),
            logger: logger.Object
        );

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(500, context.Response.StatusCode);
    }

    [Fact]
    public async Task ErrorHandlingMiddleware_HandlesNotFoundException()
    {
        // Arrange
        var logger = new Mock<ILogger<ErrorHandlingMiddleware>>();
        var middleware = new ErrorHandlingMiddleware(
            next: (context) => throw new FileNotFoundException("Not found"),
            logger: logger.Object
        );

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(500, context.Response.StatusCode);
    }

    [Fact]
    public async Task ErrorHandlingMiddleware_HandlesSuccessfulRequest()
    {
        // Arrange
        var logger = new Mock<ILogger<ErrorHandlingMiddleware>>();
        var middleware = new ErrorHandlingMiddleware(
            next: async (context) => 
            {
                context.Response.StatusCode = 200;
                await context.Response.WriteAsync("Success");
            },
            logger: logger.Object
        );

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(200, context.Response.StatusCode);
    }

    [Fact]
    public async Task ErrorHandlingMiddleware_Constructor_InitializesCorrectly()
    {
        // Arrange
        var logger = new Mock<ILogger<ErrorHandlingMiddleware>>();
        RequestDelegate next = (context) => Task.CompletedTask;

        // Act
        var middleware = new ErrorHandlingMiddleware(next, logger.Object);

        // Assert
        Assert.NotNull(middleware);
    }
} 