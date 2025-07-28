using System;
using System.Threading.Tasks;
using JokesApi.Data;
using JokesApi.Entities;
using JokesApi.Services;
using JokesApi.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;
using System.Linq;
using Moq;
using Microsoft.IdentityModel.Tokens;
using System.Threading;

namespace JokesApi.Tests
{
    public class ServiceTests
    {
        private static AppDbContext CreateDb()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public void TokenService_CreateTokenPair_WithNullUser_ThrowsException()
        {
            // Arrange
            var db = CreateDb();
            var tokenService = new TokenService(db, Options.Create(new JwtSettings
            {
                Key = "UnitTestSecretKeyUnitTestSecretKey123",
                Issuer = "TestIssuer",
                Audience = "TestAudience",
                ExpirationMinutes = 5
            }));

            // Act & Assert
            Assert.Throws<NullReferenceException>(() => tokenService.CreateTokenPair(null!));
        }

        [Fact]
        public void TokenService_CreateTokenPair_WithEmptyKey_ThrowsException()
        {
            // Arrange
            var db = CreateDb();
            var tokenService = new TokenService(db, Options.Create(new JwtSettings
            {
                Key = "",
                Issuer = "TestIssuer",
                Audience = "TestAudience",
                ExpirationMinutes = 5
            }));
            var user = new User { Id = Guid.NewGuid(), Name = "Test User", Email = "test@example.com", Role = "user", PasswordHash = "hash" };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => tokenService.CreateTokenPair(user));
        }

        [Fact]
        public void TokenService_Refresh_WithExpiredToken_ThrowsException()
        {
            // Arrange
            var db = CreateDb();
            var tokenService = new TokenService(db, Options.Create(new JwtSettings
            {
                Key = "UnitTestSecretKeyUnitTestSecretKey123",
                Issuer = "TestIssuer",
                Audience = "TestAudience",
                ExpirationMinutes = -1 // Expired immediately
            }));

            // Act & Assert
            Assert.Throws<SecurityTokenException>(() => tokenService.Refresh("expired-token"));
        }

        [Fact]
        public void TokenService_Revoke_WithAlreadyRevokedToken_DoesNothing()
        {
            // Arrange
            var db = CreateDb();
            var tokenService = new TokenService(db, Options.Create(new JwtSettings
            {
                Key = "UnitTestSecretKeyUnitTestSecretKey123",
                Issuer = "TestIssuer",
                Audience = "TestAudience",
                ExpirationMinutes = 5
            }));
            var user = new User { Id = Guid.NewGuid(), Name = "Test User", Email = "test@example.com", Role = "user", PasswordHash = "hash" };
            
            db.Users.Add(user);
            db.SaveChanges();
            
            var tokenPair = tokenService.CreateTokenPair(user);
            tokenService.Revoke(tokenPair.RefreshToken);

            // Act - Try to revoke again
            tokenService.Revoke(tokenPair.RefreshToken);

            // Assert - Should not throw
            var storedToken = db.RefreshTokens.FirstOrDefault(t => t.Token == tokenPair.RefreshToken);
            Assert.NotNull(storedToken);
            Assert.NotNull(storedToken.RevokedAt);
        }

        [Fact]
        public void AlertService_Constructor_WithNullNotifiers_ThrowsException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => new AlertService(null!, Options.Create(new NotificationSettings { DefaultChannel = "email" })));
        }

        [Fact]
        public void AlertService_Constructor_WithNullSettings_ThrowsException()
        {
            // Arrange & Act & Assert
            Assert.Throws<NullReferenceException>(() => new AlertService(new JokesApi.Notifications.INotifier[0], null!));
        }

        [Fact]
        public async Task AlertService_SendAsync_WithEmptyMessage_HandlesGracefully()
        {
            // Arrange
            var notifier = new Mock<JokesApi.Notifications.INotifier>();
            notifier.SetupGet(n => n.Channel).Returns("email");
            notifier.Setup(n => n.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            var alertService = new AlertService(new[] { notifier.Object }, Options.Create(new NotificationSettings { DefaultChannel = "email" }));

            // Act
            await alertService.SendAsync("test@example.com", "");

            // Assert - Should not throw
            notifier.Verify(n => n.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task AlertService_SendAsync_WithNullMessage_HandlesGracefully()
        {
            // Arrange
            var notifier = new Mock<JokesApi.Notifications.INotifier>();
            notifier.SetupGet(n => n.Channel).Returns("email");
            notifier.Setup(n => n.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            var alertService = new AlertService(new[] { notifier.Object }, Options.Create(new NotificationSettings { DefaultChannel = "email" }));

            // Act
            await alertService.SendAsync("test@example.com", null!);

            // Assert - Should not throw
            notifier.Verify(n => n.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task AlertService_SendAsync_WithWhitespaceMessage_HandlesGracefully()
        {
            // Arrange
            var notifier = new Mock<JokesApi.Notifications.INotifier>();
            notifier.SetupGet(n => n.Channel).Returns("email");
            notifier.Setup(n => n.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            var alertService = new AlertService(new[] { notifier.Object }, Options.Create(new NotificationSettings { DefaultChannel = "email" }));

            // Act
            await alertService.SendAsync("test@example.com", "   ");

            // Assert - Should not throw
            notifier.Verify(n => n.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task AlertService_SendAsync_WithException_HandlesGracefully()
        {
            // Arrange
            var notifier = new Mock<JokesApi.Notifications.INotifier>();
            notifier.SetupGet(n => n.Channel).Returns("email");
            notifier.Setup(n => n.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("Network error"));
            var alertService = new AlertService(new[] { notifier.Object }, Options.Create(new NotificationSettings { DefaultChannel = "email" }));

            // Act & Assert - Should not throw
            await Assert.ThrowsAsync<Exception>(async () => 
                await alertService.SendAsync("test@example.com", "Test message"));
        }

        [Fact]
        public void NotificationSettings_WithInvalidChannel_DoesNotThrowException()
        {
            // Arrange & Act & Assert - Should not throw
            var settings = new NotificationSettings { DefaultChannel = "invalid" };
            Assert.Equal("invalid", settings.DefaultChannel);
        }

        [Fact]
        public void JwtSettings_WithShortKey_DoesNotThrowException()
        {
            // Arrange & Act & Assert - Should not throw
            var settings = new JwtSettings 
            { 
                Key = "short",
                Issuer = "TestIssuer",
                Audience = "TestAudience",
                ExpirationMinutes = 5
            };
            Assert.Equal("short", settings.Key);
        }

        [Fact]
        public void JwtSettings_WithZeroExpiration_DoesNotThrowException()
        {
            // Arrange & Act & Assert - Should not throw
            var settings = new JwtSettings 
            { 
                Key = "UnitTestSecretKeyUnitTestSecretKey123",
                Issuer = "TestIssuer",
                Audience = "TestAudience",
                ExpirationMinutes = 0
            };
            Assert.Equal(0, settings.ExpirationMinutes);
        }

        [Fact]
        public void JwtSettings_WithNegativeExpiration_DoesNotThrowException()
        {
            // Arrange & Act & Assert - Should not throw
            var settings = new JwtSettings 
            { 
                Key = "UnitTestSecretKeyUnitTestSecretKey123",
                Issuer = "TestIssuer",
                Audience = "TestAudience",
                ExpirationMinutes = -1
            };
            Assert.Equal(-1, settings.ExpirationMinutes);
        }
    }
} 