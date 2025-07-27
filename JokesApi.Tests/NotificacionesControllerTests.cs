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
} 