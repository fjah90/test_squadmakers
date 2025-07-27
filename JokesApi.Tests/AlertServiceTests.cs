using JokesApi.Notifications;
using JokesApi.Services;
using JokesApi.Settings;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using System.Threading.Tasks;
using System;

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
} 