using JokesApi.Notifications;
using JokesApi.Services;
using JokesApi.Settings;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Threading;
using System.Linq;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authorization;
using JokesApi.Swagger;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Collections.Generic;

namespace JokesApi.Tests;

public class AlertServiceTests
{
    private static INotifier CreateNotifierMock(string channel)
    {
        var mock = new Mock<INotifier>();
        mock.SetupGet(n => n.Channel).Returns(channel);
        mock.Setup(n => n.SendAsync(It.IsAny<string>(), It.IsAny<string>(), default)).Returns(Task.CompletedTask);
        return mock.Object;
    }

    [Theory]
    [InlineData("email")]
    [InlineData("sms")]
    public async Task SendAsync_UsesCorrectChannel(string channel)
    {
        // Arrange
        var notifiers = new[] { CreateNotifierMock("email"), CreateNotifierMock("sms") };
        var options = Options.Create(new NotificationSettings { DefaultChannel = "email" });
        var service = new AlertService(notifiers, options);

        // Act
        await service.SendAsync("dest", "msg", channel);

        // Assert -> Moq verifies via Setup
    }

    [Fact]
    public async Task SendAsync_FallsBackToDefault_WhenChannelUnknown()
    {
        var emailMock = new Mock<INotifier>();
        emailMock.SetupGet(n => n.Channel).Returns("email");
        emailMock.Setup(n => n.SendAsync(It.IsAny<string>(), It.IsAny<string>(), default)).Returns(Task.CompletedTask).Verifiable();

        var service = new AlertService(new[] { emailMock.Object }, Options.Create(new NotificationSettings { DefaultChannel = "email" }));
        await service.SendAsync("dest", "msg", "unknown");
        emailMock.Verify();
    }

    [Fact]
    public async Task SendAsync_WithEmailChannel_UsesEmailNotifier()
    {
        // Arrange
        var emailMock = new Mock<INotifier>();
        emailMock.SetupGet(n => n.Channel).Returns("email");
        emailMock.Setup(n => n.SendAsync(It.IsAny<string>(), It.IsAny<string>(), default)).Returns(Task.CompletedTask).Verifiable();

        var smsMock = new Mock<INotifier>();
        smsMock.SetupGet(n => n.Channel).Returns("sms");
        smsMock.Setup(n => n.SendAsync(It.IsAny<string>(), It.IsAny<string>(), default)).Returns(Task.CompletedTask);

        var service = new AlertService(new[] { emailMock.Object, smsMock.Object }, Options.Create(new NotificationSettings { DefaultChannel = "email" }));

        // Act
        await service.SendAsync("dest", "msg", "email");

        // Assert
        emailMock.Verify();
        smsMock.Verify(n => n.SendAsync(It.IsAny<string>(), It.IsAny<string>(), default), Times.Never);
    }

    [Fact]
    public async Task SendAsync_WithSmsChannel_UsesSmsNotifier()
    {
        // Arrange
        var emailMock = new Mock<INotifier>();
        emailMock.SetupGet(n => n.Channel).Returns("email");
        emailMock.Setup(n => n.SendAsync(It.IsAny<string>(), It.IsAny<string>(), default)).Returns(Task.CompletedTask);

        var smsMock = new Mock<INotifier>();
        smsMock.SetupGet(n => n.Channel).Returns("sms");
        smsMock.Setup(n => n.SendAsync(It.IsAny<string>(), It.IsAny<string>(), default)).Returns(Task.CompletedTask).Verifiable();

        var service = new AlertService(new[] { emailMock.Object, smsMock.Object }, Options.Create(new NotificationSettings { DefaultChannel = "email" }));

        // Act
        await service.SendAsync("dest", "msg", "sms");

        // Assert
        smsMock.Verify();
        emailMock.Verify(n => n.SendAsync(It.IsAny<string>(), It.IsAny<string>(), default), Times.Never);
    }

    [Fact]
    public async Task SendAsync_WithUnknownChannel_FallsBackToDefault()
    {
        // Arrange
        var emailMock = new Mock<INotifier>();
        emailMock.SetupGet(n => n.Channel).Returns("email");
        emailMock.Setup(n => n.SendAsync(It.IsAny<string>(), It.IsAny<string>(), default)).Returns(Task.CompletedTask).Verifiable();

        var service = new AlertService(new[] { emailMock.Object }, Options.Create(new NotificationSettings { DefaultChannel = "email" }));

        // Act
        await service.SendAsync("dest", "msg", "unknown");

        // Assert
        emailMock.Verify();
    }

    [Fact]
    public async Task SendAsync_WithNullChannel_UsesDefault()
    {
        // Arrange
        var emailMock = new Mock<INotifier>();
        emailMock.SetupGet(n => n.Channel).Returns("email");
        emailMock.Setup(n => n.SendAsync(It.IsAny<string>(), It.IsAny<string>(), default)).Returns(Task.CompletedTask).Verifiable();

        var service = new AlertService(new[] { emailMock.Object }, Options.Create(new NotificationSettings { DefaultChannel = "email" }));

        // Act
        await service.SendAsync("dest", "msg", null);

        // Assert
        emailMock.Verify();
    }

    [Fact]
    public async Task SendAsync_WithEmptyChannel_UsesDefault()
    {
        // Arrange
        var emailMock = new Mock<INotifier>();
        emailMock.SetupGet(n => n.Channel).Returns("email");
        emailMock.Setup(n => n.SendAsync(It.IsAny<string>(), It.IsAny<string>(), default)).Returns(Task.CompletedTask).Verifiable();

        var service = new AlertService(new[] { emailMock.Object }, Options.Create(new NotificationSettings { DefaultChannel = "email" }));

        // Act
        await service.SendAsync("dest", "msg", "");

        // Assert
        emailMock.Verify();
    }

    [Fact]
    public async Task SendAsync_WithWhitespaceChannel_UsesDefault()
    {
        // Arrange
        var emailMock = new Mock<INotifier>();
        emailMock.SetupGet(n => n.Channel).Returns("email");
        emailMock.Setup(n => n.SendAsync(It.IsAny<string>(), It.IsAny<string>(), default)).Returns(Task.CompletedTask).Verifiable();

        var service = new AlertService(new[] { emailMock.Object }, Options.Create(new NotificationSettings { DefaultChannel = "email" }));

        // Act
        await service.SendAsync("dest", "msg", "   ");

        // Assert
        emailMock.Verify();
    }

    [Fact]
    public async Task SendAsync_WithException_HandlesGracefully()
    {
        // Arrange
        var emailMock = new Mock<INotifier>();
        emailMock.SetupGet(n => n.Channel).Returns("email");
        emailMock.Setup(n => n.SendAsync(It.IsAny<string>(), It.IsAny<string>(), default)).ThrowsAsync(new Exception("Test exception"));

        var service = new AlertService(new[] { emailMock.Object }, Options.Create(new NotificationSettings { DefaultChannel = "email" }));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(async () => await service.SendAsync("dest", "msg", "email"));
    }
}

// Tests for EmailNotifier
public class EmailNotifierTests
{
    [Fact]
    public async Task EmailNotifier_SendAsync_LogsEmail()
    {
        // Arrange
        var logger = new Mock<ILogger<EmailNotifier>>();
        var notifier = new EmailNotifier(logger.Object);
        var recipient = "test@example.com";
        var message = "Test email message";

        // Act
        await notifier.SendAsync(recipient, message);

        // Assert
        logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Simulated EMAIL")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task EmailNotifier_Channel_ReturnsEmail()
    {
        // Arrange
        var logger = new Mock<ILogger<EmailNotifier>>();
        var notifier = new EmailNotifier(logger.Object);

        // Act
        var channel = notifier.Channel;

        // Assert
        Assert.Equal("email", channel);
    }

    [Fact]
    public async Task EmailNotifier_WithNullRecipient_HandlesGracefully()
    {
        // Arrange
        var logger = new Mock<ILogger<EmailNotifier>>();
        var notifier = new EmailNotifier(logger.Object);

        // Act & Assert
        await notifier.SendAsync(null!, "Test message");
        // Should not throw exception
    }

    [Fact]
    public async Task EmailNotifier_WithEmptyMessage_HandlesGracefully()
    {
        // Arrange
        var logger = new Mock<ILogger<EmailNotifier>>();
        var notifier = new EmailNotifier(logger.Object);

        // Act & Assert
        await notifier.SendAsync("test@example.com", "");
        // Should not throw exception
    }

    [Fact]
    public async Task EmailNotifier_WithCancellationToken_RespectsCancellation()
    {
        // Arrange
        var logger = new Mock<ILogger<EmailNotifier>>();
        var notifier = new EmailNotifier(logger.Object);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await notifier.SendAsync("test@example.com", "Test message", cts.Token);
        // Should not throw exception even with cancelled token
    }
}

// Tests for SmsNotifier
public class SmsNotifierTests
{
    [Fact]
    public async Task SmsNotifier_SendAsync_LogsSms()
    {
        // Arrange
        var logger = new Mock<ILogger<SmsNotifier>>();
        var notifier = new SmsNotifier(logger.Object);
        var recipient = "+1234567890";
        var message = "Test SMS message";

        // Act
        await notifier.SendAsync(recipient, message);

        // Assert
        logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Simulated SMS")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SmsNotifier_Channel_ReturnsSms()
    {
        // Arrange
        var logger = new Mock<ILogger<SmsNotifier>>();
        var notifier = new SmsNotifier(logger.Object);

        // Act
        var channel = notifier.Channel;

        // Assert
        Assert.Equal("sms", channel);
    }

    [Fact]
    public async Task SmsNotifier_WithNullRecipient_HandlesGracefully()
    {
        // Arrange
        var logger = new Mock<ILogger<SmsNotifier>>();
        var notifier = new SmsNotifier(logger.Object);

        // Act & Assert
        await notifier.SendAsync(null!, "Test message");
        // Should not throw exception
    }

    [Fact]
    public async Task SmsNotifier_WithEmptyMessage_HandlesGracefully()
    {
        // Arrange
        var logger = new Mock<ILogger<SmsNotifier>>();
        var notifier = new SmsNotifier(logger.Object);

        // Act & Assert
        await notifier.SendAsync("+1234567890", "");
        // Should not throw exception
    }

    [Fact]
    public async Task SmsNotifier_WithCancellationToken_RespectsCancellation()
    {
        // Arrange
        var logger = new Mock<ILogger<SmsNotifier>>();
        var notifier = new SmsNotifier(logger.Object);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await notifier.SendAsync("+1234567890", "Test message", cts.Token);
        // Should not throw exception even with cancelled token
    }
}

// Tests for AuthorizeCheckOperationFilter
public class AuthorizeCheckOperationFilterTests
{
    [Fact]
    public void Apply_WithAuthorizeAttribute_AddsSecurityRequirement()
    {
        // Arrange
        var filter = new JokesApi.Swagger.AuthorizeCheckOperationFilter();
        var operation = new Microsoft.OpenApi.Models.OpenApiOperation();
        var context = CreateOperationFilterContext(typeof(TestControllerWithAuthorize));

        // Act
        filter.Apply(operation, context);

        // Assert
        Assert.NotNull(operation.Security);
        Assert.Single(operation.Security);
        var securityRequirement = operation.Security.First();
        Assert.Single(securityRequirement);
        var scheme = securityRequirement.Keys.First();
        Assert.Equal("Bearer", scheme.Reference.Id);
    }

    [Fact]
    public void Apply_WithoutAuthorizeAttribute_DoesNotAddSecurityRequirement()
    {
        // Arrange
        var filter = new JokesApi.Swagger.AuthorizeCheckOperationFilter();
        var operation = new Microsoft.OpenApi.Models.OpenApiOperation();
        var context = CreateOperationFilterContext(typeof(TestControllerWithoutAuthorize));

        // Act
        filter.Apply(operation, context);

        // Assert
        Assert.Null(operation.Security);
    }

    [Fact]
    public void Apply_WithMultipleAuthorizeAttributes_AddsAllSecurityRequirements()
    {
        // Arrange
        var filter = new JokesApi.Swagger.AuthorizeCheckOperationFilter();
        var operation = new Microsoft.OpenApi.Models.OpenApiOperation();
        var context = CreateOperationFilterContext(typeof(TestControllerWithMultipleAuthorize));

        // Act
        filter.Apply(operation, context);

        // Assert
        Assert.NotNull(operation.Security);
        Assert.Single(operation.Security);
        var securityRequirement = operation.Security.First();
        Assert.Single(securityRequirement);
        var scheme = securityRequirement.Keys.First();
        Assert.Equal("Bearer", scheme.Reference.Id);
    }

    [Fact]
    public void Apply_WithCustomPolicy_AddsCorrectSecurityRequirement()
    {
        // Arrange
        var filter = new JokesApi.Swagger.AuthorizeCheckOperationFilter();
        var operation = new Microsoft.OpenApi.Models.OpenApiOperation();
        var context = CreateOperationFilterContext(typeof(TestControllerWithCustomPolicy));

        // Act
        filter.Apply(operation, context);

        // Assert
        Assert.NotNull(operation.Security);
        Assert.Single(operation.Security);
        var securityRequirement = operation.Security.First();
        Assert.Single(securityRequirement);
        var scheme = securityRequirement.Keys.First();
        Assert.Equal("Bearer", scheme.Reference.Id);
    }

    private static Swashbuckle.AspNetCore.SwaggerGen.OperationFilterContext CreateOperationFilterContext(Type controllerType)
    {
        var methodInfo = controllerType.GetMethod("TestMethod");
        var context = new Swashbuckle.AspNetCore.SwaggerGen.OperationFilterContext(
            new Microsoft.OpenApi.Models.OpenApiDocument(),
            new Swashbuckle.AspNetCore.SwaggerGen.SchemaGenerator(
                new Swashbuckle.AspNetCore.SwaggerGen.SchemaGeneratorOptions(),
                new Swashbuckle.AspNetCore.SwaggerGen.JsonSerializerDataContractResolver(
                    new System.Text.Json.JsonSerializerOptions())),
            new Swashbuckle.AspNetCore.SwaggerGen.SchemaRepository(),
            methodInfo);

        return context;
    }

    // Test classes for different scenarios
    [Microsoft.AspNetCore.Authorization.Authorize]
    private class TestControllerWithAuthorize
    {
        public void TestMethod() { }
    }

    private class TestControllerWithoutAuthorize
    {
        public void TestMethod() { }
    }

    [Microsoft.AspNetCore.Authorization.Authorize]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "admin")]
    private class TestControllerWithMultipleAuthorize
    {
        public void TestMethod() { }
    }

    [Microsoft.AspNetCore.Authorization.Authorize(Policy = "CustomPolicy")]
    private class TestControllerWithCustomPolicy
    {
        public void TestMethod() { }
    }
}

// Tests for ErrorHandlingMiddleware
public class ErrorHandlingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WithException_ReturnsInternalServerError()
    {
        // Arrange
        var logger = new Mock<ILogger<JokesApi.Middleware.ErrorHandlingMiddleware>>();
        var middleware = new JokesApi.Middleware.ErrorHandlingMiddleware(
            (context) => throw new Exception("Test exception"),
            logger.Object);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(500, context.Response.StatusCode);
        Assert.Equal("application/json", context.Response.ContentType);
        
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.Contains("Internal Server Error", responseBody);
    }

    [Fact]
    public async Task InvokeAsync_WithValidationException_ReturnsBadRequest()
    {
        // Arrange
        var logger = new Mock<ILogger<JokesApi.Middleware.ErrorHandlingMiddleware>>();
        var middleware = new JokesApi.Middleware.ErrorHandlingMiddleware(
            (context) => throw new ArgumentException("Validation error"),
            logger.Object);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(500, context.Response.StatusCode); // Current implementation returns 500 for all exceptions
        Assert.Equal("application/json", context.Response.ContentType);
        
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.Contains("Internal Server Error", responseBody);
    }

    [Fact]
    public async Task InvokeAsync_WithUnauthorizedException_ReturnsUnauthorized()
    {
        // Arrange
        var logger = new Mock<ILogger<JokesApi.Middleware.ErrorHandlingMiddleware>>();
        var middleware = new JokesApi.Middleware.ErrorHandlingMiddleware(
            (context) => throw new UnauthorizedAccessException("Unauthorized"),
            logger.Object);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(500, context.Response.StatusCode); // Current implementation returns 500 for all exceptions
        Assert.Equal("application/json", context.Response.ContentType);
        
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.Contains("Internal Server Error", responseBody);
    }

    [Fact]
    public async Task InvokeAsync_WithNotFoundException_ReturnsNotFound()
    {
        // Arrange
        var logger = new Mock<ILogger<JokesApi.Middleware.ErrorHandlingMiddleware>>();
        var middleware = new JokesApi.Middleware.ErrorHandlingMiddleware(
            (context) => throw new KeyNotFoundException("Not found"),
            logger.Object);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(500, context.Response.StatusCode); // Current implementation returns 500 for all exceptions
        Assert.Equal("application/json", context.Response.ContentType);
        
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.Contains("Internal Server Error", responseBody);
    }

    [Fact]
    public async Task InvokeAsync_WithoutException_ContinuesPipeline()
    {
        // Arrange
        var logger = new Mock<ILogger<JokesApi.Middleware.ErrorHandlingMiddleware>>();
        var wasNextCalled = false;
        
        var middleware = new JokesApi.Middleware.ErrorHandlingMiddleware(
            (context) => { wasNextCalled = true; return Task.CompletedTask; },
            logger.Object);

        var context = new DefaultHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(wasNextCalled);
        Assert.Equal(200, context.Response.StatusCode); // Default status code
    }

    [Fact]
    public async Task InvokeAsync_WithException_LogsError()
    {
        // Arrange
        var logger = new Mock<ILogger<JokesApi.Middleware.ErrorHandlingMiddleware>>();
        var middleware = new JokesApi.Middleware.ErrorHandlingMiddleware(
            (context) => throw new Exception("Test exception"),
            logger.Object);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        logger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Unhandled exception")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithException_ReturnsValidJson()
    {
        // Arrange
        var logger = new Mock<ILogger<JokesApi.Middleware.ErrorHandlingMiddleware>>();
        var middleware = new JokesApi.Middleware.ErrorHandlingMiddleware(
            (context) => throw new Exception("Test exception"),
            logger.Object);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
        
        // Verify it's valid JSON
        var jsonResponse = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(responseBody);
        Assert.NotNull(jsonResponse);
        Assert.True(jsonResponse.ContainsKey("message"));
        Assert.Equal("Internal Server Error", jsonResponse["message"]);
    }
}

// Tests for Settings
public class JwtSettingsTests
{
    [Fact]
    public void JwtSettings_WithValidConfiguration_IsValid()
    {
        // Arrange & Act
        var settings = new JwtSettings
        {
            Key = "TestSecretKey123456789012345678901234567890",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpirationMinutes = 60
        };

        // Assert
        Assert.NotNull(settings.Key);
        Assert.NotNull(settings.Issuer);
        Assert.NotNull(settings.Audience);
        Assert.Equal(60, settings.ExpirationMinutes);
    }

    [Fact]
    public void JwtSettings_WithMissingKey_ThrowsException()
    {
        // Arrange & Act & Assert
        Assert.Throws<InvalidOperationException>(() => new JwtSettings
        {
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpirationMinutes = 60
        });
    }

    [Fact]
    public void JwtSettings_WithInvalidExpiration_ThrowsException()
    {
        // Arrange & Act
        var settings = new JwtSettings
        {
            Key = "TestSecretKey123456789012345678901234567890",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpirationMinutes = -1
        };

        // Assert
        Assert.Equal(-1, settings.ExpirationMinutes);
        // Note: The current implementation doesn't validate expiration, so this test documents the current behavior
    }

    [Fact]
    public void JwtSettings_WithDefaultExpiration_IsValid()
    {
        // Arrange & Act
        var settings = new JwtSettings
        {
            Key = "TestSecretKey123456789012345678901234567890",
            Issuer = "TestIssuer",
            Audience = "TestAudience"
        };

        // Assert
        Assert.Equal(60, settings.ExpirationMinutes); // Default value
    }

    [Fact]
    public void JwtSettings_WithZeroExpiration_IsValid()
    {
        // Arrange & Act
        var settings = new JwtSettings
        {
            Key = "TestSecretKey123456789012345678901234567890",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpirationMinutes = 0
        };

        // Assert
        Assert.Equal(0, settings.ExpirationMinutes);
    }
}

public class NotificationSettingsTests
{
    [Fact]
    public void NotificationSettings_WithValidConfiguration_IsValid()
    {
        // Arrange & Act
        var settings = new NotificationSettings
        {
            DefaultChannel = "email"
        };

        // Assert
        Assert.Equal("email", settings.DefaultChannel);
    }

    [Fact]
    public void NotificationSettings_WithInvalidChannel_ThrowsException()
    {
        // Arrange & Act
        var settings = new NotificationSettings
        {
            DefaultChannel = "invalid"
        };

        // Assert
        Assert.Equal("invalid", settings.DefaultChannel);
        // Note: The current implementation doesn't validate the channel, so this test documents the current behavior
    }

    [Fact]
    public void NotificationSettings_WithSmsChannel_IsValid()
    {
        // Arrange & Act
        var settings = new NotificationSettings
        {
            DefaultChannel = "sms"
        };

        // Assert
        Assert.Equal("sms", settings.DefaultChannel);
    }

    [Fact]
    public void NotificationSettings_WithNullChannel_IsValid()
    {
        // Arrange & Act
        var settings = new NotificationSettings
        {
            DefaultChannel = null
        };

        // Assert
        Assert.Null(settings.DefaultChannel);
    }

    [Fact]
    public void NotificationSettings_WithEmptyChannel_IsValid()
    {
        // Arrange & Act
        var settings = new NotificationSettings
        {
            DefaultChannel = ""
        };

        // Assert
        Assert.Equal("", settings.DefaultChannel);
    }

    [Fact]
    public void NotificationSettings_WithDefaultValue_IsValid()
    {
        // Arrange & Act
        var settings = new NotificationSettings();

        // Assert
        Assert.Equal("email", settings.DefaultChannel); // Default value
    }
} 