using JokesApi.Controllers;
using JokesApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using JokesApi.Settings;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using System;

namespace JokesApi.Tests;

public class NotificacionesControllerTests
{
    [Fact]
    public async Task Send_ReturnsOk()
    {
        var notifier = new Mock<JokesApi.Notifications.INotifier>();
        notifier.SetupGet(n => n.Channel).Returns("email");
        var alert = new AlertService(new[] { notifier.Object }, Microsoft.Extensions.Options.Options.Create(new JokesApi.Settings.NotificationSettings { DefaultChannel = "email" }));
        var ctrl = new NotificacionesController(alert, NullLogger<NotificacionesController>.Instance);
        // override SendAsync via notifier mock
        notifier.Setup(n => n.SendAsync(It.IsAny<string>(), It.IsAny<string>(), default)).Returns(Task.CompletedTask);
        var res = await ctrl.Send(new NotificacionesController.SendNotificationRequest("dest","hi","email")) as OkObjectResult;
        Assert.NotNull(res);
    }

    [Fact]
    public async Task Send_WithValidRequest_ReturnsOk()
    {
        // Arrange
        var notifier = new Mock<JokesApi.Notifications.INotifier>();
        notifier.SetupGet(n => n.Channel).Returns("email");
        notifier.Setup(n => n.SendAsync(It.IsAny<string>(), It.IsAny<string>(), default)).Returns(Task.CompletedTask);
        
        var alert = new AlertService(new[] { notifier.Object }, Options.Create(new NotificationSettings { DefaultChannel = "email" }));
        var controller = new NotificacionesController(alert, NullLogger<NotificacionesController>.Instance);
        
        var request = new NotificacionesController.SendNotificationRequest("test@example.com", "Test message", "email");

        // Act
        var result = await controller.Send(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task Send_WithSmsChannel_ReturnsOk()
    {
        // Arrange
        var notifier = new Mock<JokesApi.Notifications.INotifier>();
        notifier.SetupGet(n => n.Channel).Returns("sms");
        notifier.Setup(n => n.SendAsync(It.IsAny<string>(), It.IsAny<string>(), default)).Returns(Task.CompletedTask);
        
        var alert = new AlertService(new[] { notifier.Object }, Options.Create(new NotificationSettings { DefaultChannel = "sms" }));
        var controller = new NotificacionesController(alert, NullLogger<NotificacionesController>.Instance);
        
        var request = new NotificacionesController.SendNotificationRequest("+1234567890", "Test SMS", "sms");

        // Act
        var result = await controller.Send(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task Send_WithNullRequest_ReturnsBadRequest()
    {
        // Arrange
        var notifier = new Mock<JokesApi.Notifications.INotifier>();
        notifier.SetupGet(n => n.Channel).Returns("email");
        
        var alert = new AlertService(new[] { notifier.Object }, Options.Create(new NotificationSettings { DefaultChannel = "email" }));
        var controller = new NotificacionesController(alert, NullLogger<NotificacionesController>.Instance);

        // Act
        var result = await controller.Send(null!);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Send_WithEmptyRecipient_ReturnsBadRequest()
    {
        // Arrange
        var notifier = new Mock<JokesApi.Notifications.INotifier>();
        notifier.SetupGet(n => n.Channel).Returns("email");
        
        var alert = new AlertService(new[] { notifier.Object }, Options.Create(new NotificationSettings { DefaultChannel = "email" }));
        var controller = new NotificacionesController(alert, NullLogger<NotificacionesController>.Instance);
        
        var request = new NotificacionesController.SendNotificationRequest("", "Test message", "email");

        // Act
        var result = await controller.Send(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Send_WithEmptyMessage_ReturnsBadRequest()
    {
        // Arrange
        var notifier = new Mock<JokesApi.Notifications.INotifier>();
        notifier.SetupGet(n => n.Channel).Returns("email");
        
        var alert = new AlertService(new[] { notifier.Object }, Options.Create(new NotificationSettings { DefaultChannel = "email" }));
        var controller = new NotificacionesController(alert, NullLogger<NotificacionesController>.Instance);
        
        var request = new NotificacionesController.SendNotificationRequest("test@example.com", "", "email");

        // Act
        var result = await controller.Send(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Send_WithException_Returns500()
    {
        // Arrange
        var notifier = new Mock<JokesApi.Notifications.INotifier>();
        notifier.SetupGet(n => n.Channel).Returns("email");
        notifier.Setup(n => n.SendAsync(It.IsAny<string>(), It.IsAny<string>(), default)).ThrowsAsync(new Exception("Test exception"));
        
        var alert = new AlertService(new[] { notifier.Object }, Options.Create(new NotificationSettings { DefaultChannel = "email" }));
        var controller = new NotificacionesController(alert, NullLogger<NotificacionesController>.Instance);
        
        var request = new NotificacionesController.SendNotificationRequest("test@example.com", "Test message", "email");

        // Act
        var result = await controller.Send(request);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task Send_WithNullChannel_UsesDefault()
    {
        // Arrange
        var notifier = new Mock<JokesApi.Notifications.INotifier>();
        notifier.SetupGet(n => n.Channel).Returns("email");
        notifier.Setup(n => n.SendAsync(It.IsAny<string>(), It.IsAny<string>(), default)).Returns(Task.CompletedTask);
        
        var alert = new AlertService(new[] { notifier.Object }, Options.Create(new NotificationSettings { DefaultChannel = "email" }));
        var controller = new NotificacionesController(alert, NullLogger<NotificacionesController>.Instance);
        
        var request = new NotificacionesController.SendNotificationRequest("test@example.com", "Test message", null);

        // Act
        var result = await controller.Send(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task Send_WithEmptyChannel_UsesDefault()
    {
        // Arrange
        var notifier = new Mock<JokesApi.Notifications.INotifier>();
        notifier.SetupGet(n => n.Channel).Returns("email");
        notifier.Setup(n => n.SendAsync(It.IsAny<string>(), It.IsAny<string>(), default)).Returns(Task.CompletedTask);
        
        var alert = new AlertService(new[] { notifier.Object }, Options.Create(new NotificationSettings { DefaultChannel = "email" }));
        var controller = new NotificacionesController(alert, NullLogger<NotificacionesController>.Instance);
        
        var request = new NotificacionesController.SendNotificationRequest("test@example.com", "Test message", "");

        // Act
        var result = await controller.Send(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }
} 