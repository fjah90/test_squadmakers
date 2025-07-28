using JokesApi.Controllers;
using JokesApi.Data;
using JokesApi.Entities;
using JokesApi.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using System.Security.Principal;
using Xunit;

namespace JokesApi.Tests
{
    public class AuthControllerOAuthTests
    {
        private AppDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public void GoogleLogin_RedirectsToGoogle()
        {
            // Arrange
            var context = CreateContext();
            var tokenService = new Mock<ITokenService>();
            var logger = new Mock<ILogger<AuthController>>();
            var controller = new AuthController(context, tokenService.Object, logger.Object);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            // Act
            var result = controller.GoogleLogin("/test-return");

            // Assert
            var challengeResult = Assert.IsType<ChallengeResult>(result);
            Assert.Contains("Google", challengeResult.AuthenticationSchemes);
        }

        [Fact]
        public void GitHubLogin_RedirectsToGitHub()
        {
            // Arrange
            var context = CreateContext();
            var tokenService = new Mock<ITokenService>();
            var logger = new Mock<ILogger<AuthController>>();
            var controller = new AuthController(context, tokenService.Object, logger.Object);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            // Act
            var result = controller.GitHubLogin("/test-return");

            // Assert
            var challengeResult = Assert.IsType<ChallengeResult>(result);
            Assert.Contains("GitHub", challengeResult.AuthenticationSchemes);
        }

        [Fact]
        public async Task ExternalCallback_WithValidGoogleToken_CreatesUser()
        {
            // Arrange
            var context = CreateContext();
            var tokenService = new Mock<ITokenService>();
            var logger = new Mock<ILogger<AuthController>>();
            var controller = new AuthController(context, tokenService.Object, logger.Object);

            // Mock authentication result
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, "test@google.com"),
                new Claim(ClaimTypes.Name, "Test User")
            };
            var identity = new ClaimsIdentity(claims, "Google");
            var principal = new ClaimsPrincipal(identity);

            var authResult = AuthenticateResult.Success(new AuthenticationTicket(principal, "Google"));

            var httpContext = new DefaultHttpContext();
            var authService = new Mock<IAuthenticationService>();
            authService.Setup(x => x.AuthenticateAsync(httpContext, null))
                .ReturnsAsync(authResult);

            httpContext.RequestServices = new Mock<IServiceProvider>()
                .Setup(x => x.GetService(typeof(IAuthenticationService)))
                .Returns(authService.Object)
                .Object;

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            tokenService.Setup(x => x.CreateTokenPair(It.IsAny<User>()))
                .Returns(new TokenPair("token", "refresh"));

            // Act
            var result = await controller.ExternalCallback("/test-return");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task ExternalCallback_WithValidGitHubToken_CreatesUser()
        {
            // Arrange
            var context = CreateContext();
            var tokenService = new Mock<ITokenService>();
            var logger = new Mock<ILogger<AuthController>>();
            var controller = new AuthController(context, tokenService.Object, logger.Object);

            // Mock authentication result
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, "test@github.com"),
                new Claim(ClaimTypes.Name, "GitHub User")
            };
            var identity = new ClaimsIdentity(claims, "GitHub");
            var principal = new ClaimsPrincipal(identity);

            var authResult = AuthenticateResult.Success(new AuthenticationTicket(principal, "GitHub"));

            var httpContext = new DefaultHttpContext();
            var authService = new Mock<IAuthenticationService>();
            authService.Setup(x => x.AuthenticateAsync(httpContext, null))
                .ReturnsAsync(authResult);

            httpContext.RequestServices = new Mock<IServiceProvider>()
                .Setup(x => x.GetService(typeof(IAuthenticationService)))
                .Returns(authService.Object)
                .Object;

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            tokenService.Setup(x => x.CreateTokenPair(It.IsAny<User>()))
                .Returns(new TokenPair("token", "refresh"));

            // Act
            var result = await controller.ExternalCallback("/test-return");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task ExternalCallback_WithExistingUser_ReturnsToken()
        {
            // Arrange
            var context = CreateContext();
            var existingUser = new User
            {
                Id = Guid.NewGuid(),
                Email = "existing@test.com",
                Name = "Existing User",
                Role = "user"
            };
            context.Users.Add(existingUser);
            await context.SaveChangesAsync();

            var tokenService = new Mock<ITokenService>();
            var logger = new Mock<ILogger<AuthController>>();
            var controller = new AuthController(context, tokenService.Object, logger.Object);

            // Mock authentication result
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, "existing@test.com"),
                new Claim(ClaimTypes.Name, "Existing User")
            };
            var identity = new ClaimsIdentity(claims, "Google");
            var principal = new ClaimsPrincipal(identity);

            var authResult = AuthenticateResult.Success(new AuthenticationTicket(principal, "Google"));

            var httpContext = new DefaultHttpContext();
            var authService = new Mock<IAuthenticationService>();
            authService.Setup(x => x.AuthenticateAsync(httpContext, null))
                .ReturnsAsync(authResult);

            httpContext.RequestServices = new Mock<IServiceProvider>()
                .Setup(x => x.GetService(typeof(IAuthenticationService)))
                .Returns(authService.Object)
                .Object;

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            tokenService.Setup(x => x.CreateTokenPair(It.IsAny<User>()))
                .Returns(new TokenPair("token", "refresh"));

            // Act
            var result = await controller.ExternalCallback("/test-return");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task ExternalCallback_WithInvalidToken_ReturnsUnauthorized()
        {
            // Arrange
            var context = CreateContext();
            var tokenService = new Mock<ITokenService>();
            var logger = new Mock<ILogger<AuthController>>();
            var controller = new AuthController(context, tokenService.Object, logger.Object);

            // Mock failed authentication
            var authResult = AuthenticateResult.Fail("Invalid token");

            var httpContext = new DefaultHttpContext();
            var authService = new Mock<IAuthenticationService>();
            authService.Setup(x => x.AuthenticateAsync(httpContext, null))
                .ReturnsAsync(authResult);

            httpContext.RequestServices = new Mock<IServiceProvider>()
                .Setup(x => x.GetService(typeof(IAuthenticationService)))
                .Returns(authService.Object)
                .Object;

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // Act
            var result = await controller.ExternalCallback("/test-return");

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task ExternalCallback_WithoutEmail_ReturnsBadRequest()
        {
            // Arrange
            var context = CreateContext();
            var tokenService = new Mock<ITokenService>();
            var logger = new Mock<ILogger<AuthController>>();
            var controller = new AuthController(context, tokenService.Object, logger.Object);

            // Mock authentication result without email
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "User Without Email")
            };
            var identity = new ClaimsIdentity(claims, "Google");
            var principal = new ClaimsPrincipal(identity);

            var authResult = AuthenticateResult.Success(new AuthenticationTicket(principal, "Google"));

            var httpContext = new DefaultHttpContext();
            var authService = new Mock<IAuthenticationService>();
            authService.Setup(x => x.AuthenticateAsync(httpContext, null))
                .ReturnsAsync(authResult);

            httpContext.RequestServices = new Mock<IServiceProvider>()
                .Setup(x => x.GetService(typeof(IAuthenticationService)))
                .Returns(authService.Object)
                .Object;

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // Act
            var result = await controller.ExternalCallback("/test-return");

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }
    }
} 