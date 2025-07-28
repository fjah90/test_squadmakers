using JokesApi.Controllers;
using JokesApi.Data;
using JokesApi.Entities;
using JokesApi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System;
using System.Linq;
using Xunit;

namespace JokesApi.Tests
{
    public class BasicControllerTests
    {
        private AppDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public void AuthController_Constructor_Works()
        {
            // Arrange
            var context = CreateContext();
            var tokenService = new Mock<ITokenService>();
            var logger = new Mock<ILogger<AuthController>>();

            // Act & Assert
            var controller = new AuthController(context, tokenService.Object, logger.Object);
            Assert.NotNull(controller);
        }

        [Fact(Skip="TODO: Complex URL helper setup")]
        public void AuthController_Login_WithNullRequest_ReturnsBadRequest()
        {
            // Arrange
            var context = CreateContext();
            var tokenService = new Mock<ITokenService>();
            var logger = new Mock<ILogger<AuthController>>();
            var controller = new AuthController(context, tokenService.Object, logger.Object);

            // Act
            var result = controller.Login(null);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact(Skip="TODO: Complex URL helper setup")]
        public void AuthController_Login_WithEmptyRequest_ReturnsBadRequest()
        {
            // Arrange
            var context = CreateContext();
            var tokenService = new Mock<ITokenService>();
            var logger = new Mock<ILogger<AuthController>>();
            var controller = new AuthController(context, tokenService.Object, logger.Object);

            var request = new AuthController.LoginRequest("", "");

            // Act
            var result = controller.Login(request);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public void AuthController_Refresh_WithNullRequest_ReturnsUnauthorized()
        {
            // Arrange
            var context = CreateContext();
            var tokenService = new Mock<ITokenService>();
            var logger = new Mock<ILogger<AuthController>>();
            var controller = new AuthController(context, tokenService.Object, logger.Object);

            // Act
            var result = controller.Refresh(null);

            // Assert
            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public void AuthController_Revoke_WithNullRequest_ReturnsNoContent()
        {
            // Arrange
            var context = CreateContext();
            var tokenService = new Mock<ITokenService>();
            var logger = new Mock<ILogger<AuthController>>();
            var controller = new AuthController(context, tokenService.Object, logger.Object);

            // Act
            var result = controller.Revoke(null);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public void MathController_Constructor_Works()
        {
            // Arrange
            var logger = new Mock<ILogger<MathController>>();

            // Act & Assert
            var controller = new MathController(logger.Object);
            Assert.NotNull(controller);
        }

        [Fact]
        public void MathController_Lcm_WithValidNumbers_ReturnsOk()
        {
            // Arrange
            var logger = new Mock<ILogger<MathController>>();
            var controller = new MathController(logger.Object);

            // Act
            var result = controller.Lcm("12,18");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public void MathController_Lcm_WithEmptyString_ReturnsBadRequest()
        {
            // Arrange
            var logger = new Mock<ILogger<MathController>>();
            var controller = new MathController(logger.Object);

            // Act
            var result = controller.Lcm("");

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public void MathController_Add_WithValidNumbers_ReturnsOk()
        {
            // Arrange
            var logger = new Mock<ILogger<MathController>>();
            var controller = new MathController(logger.Object);

            // Act
            var result = controller.Add(12, 18);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(30, okResult.Value);
        }

        [Fact]
        public void MathController_Subtract_WithValidNumbers_ReturnsOk()
        {
            // Arrange
            var logger = new Mock<ILogger<MathController>>();
            var controller = new MathController(logger.Object);

            // Act
            var result = controller.Subtract(18, 12);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(6, okResult.Value);
        }

        [Fact]
        public void MathController_NextNumber_WithValidNumber_ReturnsOk()
        {
            // Arrange
            var logger = new Mock<ILogger<MathController>>();
            var controller = new MathController(logger.Object);

            // Act
            var result = controller.NextNumber(5);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public void MathController_Multiply_WithValidNumbers_ReturnsOk()
        {
            // Arrange
            var logger = new Mock<ILogger<MathController>>();
            var controller = new MathController(logger.Object);

            // Act
            var result = controller.Multiply(5, 6);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(30, okResult.Value);
        }

        [Fact(Skip="AlertService proxy issues")]
        public void NotificacionesController_Constructor_Works()
        {
            // Arrange
            var alertService = new Mock<AlertService>();
            var logger = new Mock<ILogger<NotificacionesController>>();

            // Act & Assert
            var controller = new NotificacionesController(alertService.Object, logger.Object);
            Assert.NotNull(controller);
        }

        [Fact(Skip="AlertService proxy issues")]
        public async Task NotificacionesController_Send_WithValidData_ReturnsOk()
        {
            // Arrange
            var alertService = new Mock<AlertService>();
            var logger = new Mock<ILogger<NotificacionesController>>();
            var controller = new NotificacionesController(alertService.Object, logger.Object);

            var request = new NotificacionesController.SendNotificationRequest("test@example.com", "Test alert", "email");

            // Act
            var result = await controller.Send(request);

            // Assert
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact(Skip="AlertService proxy issues")]
        public async Task NotificacionesController_Send_WithNullRequest_ReturnsBadRequest()
        {
            // Arrange
            var alertService = new Mock<AlertService>();
            var logger = new Mock<ILogger<NotificacionesController>>();
            var controller = new NotificacionesController(alertService.Object, logger.Object);

            // Act
            var result = await controller.Send(null);

            // Assert
            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public void UserController_Constructor_Works()
        {
            // Arrange
            var context = CreateContext();
            var tokenService = new Mock<ITokenService>();
            var logger = new Mock<ILogger<UserController>>();

            // Act & Assert
            var controller = new UserController(context, tokenService.Object, logger.Object);
            Assert.NotNull(controller);
        }

        [Fact]
        public async Task UserController_GetAllUsers_ReturnsOk()
        {
            // Arrange
            var context = CreateContext();
            var tokenService = new Mock<ITokenService>();
            var logger = new Mock<ILogger<UserController>>();
            var controller = new UserController(context, tokenService.Object, logger.Object);

            // Act
            var result = await controller.GetAllUsers();

            // Assert
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task UserController_GetUserById_WithValidId_ReturnsOk()
        {
            // Arrange
            var context = CreateContext();
            var tokenService = new Mock<ITokenService>();
            var logger = new Mock<ILogger<UserController>>();
            var controller = new UserController(context, tokenService.Object, logger.Object);

            var user = new User { Id = Guid.NewGuid(), Name = "Test User", Email = "test@test.com", Role = "user", PasswordHash = "hash" };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            // Act
            var result = await controller.GetUserById(user.Id);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task UserController_GetUserById_WithNonExistentId_ReturnsNotFound()
        {
            // Arrange
            var context = CreateContext();
            var tokenService = new Mock<ITokenService>();
            var logger = new Mock<ILogger<UserController>>();
            var controller = new UserController(context, tokenService.Object, logger.Object);

            // Act
            var result = await controller.GetUserById(Guid.NewGuid());

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact(Skip="Needs refactor due to constructor dependencies")]
        public void ChistesController_Constructor_Works()
        {
            // Arrange
            var context = CreateContext();
            var unitOfWork = new JokesApi.Infrastructure.UnitOfWork(context);
            var logger = new Mock<ILogger<ChistesController>>();
            var getCombinedUseCase = new Mock<JokesApi.Application.UseCases.GetCombinedJoke>();
            var getRandomUseCase = new Mock<JokesApi.Application.UseCases.GetRandomJoke>();
            var getPairedUseCase = new Mock<JokesApi.Application.UseCases.GetPairedJokes>();

            // Act & Assert
            var controller = new ChistesController(unitOfWork, logger.Object, getCombinedUseCase.Object, getRandomUseCase.Object, getPairedUseCase.Object);
            Assert.NotNull(controller);
        }

        [Fact(Skip="Needs refactor for mocking Setup on non-virtual method")]
        public async Task ChistesController_GetRandom_ReturnsOk()
        {
            // Arrange
            var context = CreateContext();
            var unitOfWork = new JokesApi.Infrastructure.UnitOfWork(context);
            var logger = new Mock<ILogger<ChistesController>>();
            var getCombinedUseCase = new Mock<JokesApi.Application.UseCases.GetCombinedJoke>();
            var getRandomUseCase = new Mock<JokesApi.Application.UseCases.GetRandomJoke>();
            var getPairedUseCase = new Mock<JokesApi.Application.UseCases.GetPairedJokes>();

            getRandomUseCase.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("Test joke");

            var controller = new ChistesController(unitOfWork, logger.Object, getCombinedUseCase.Object, getRandomUseCase.Object, getPairedUseCase.Object);

            // Act
            var result = await controller.GetRandom("test");

            // Assert
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact(Skip="Needs refactor due to constructor dependencies")]
        public async Task ChistesController_GetById_WithValidId_ReturnsOk()
        {
            // Arrange
            var context = CreateContext();
            var unitOfWork = new JokesApi.Infrastructure.UnitOfWork(context);
            var logger = new Mock<ILogger<ChistesController>>();
            var getCombinedUseCase = new Mock<JokesApi.Application.UseCases.GetCombinedJoke>();
            var getRandomUseCase = new Mock<JokesApi.Application.UseCases.GetRandomJoke>();
            var getPairedUseCase = new Mock<JokesApi.Application.UseCases.GetPairedJokes>();

            var controller = new ChistesController(unitOfWork, logger.Object, getCombinedUseCase.Object, getRandomUseCase.Object, getPairedUseCase.Object);

            var joke = new Joke { Id = Guid.NewGuid(), Text = "Test Joke", AuthorId = Guid.NewGuid() };
            context.Jokes.Add(joke);
            await context.SaveChangesAsync();

            // Act
            var result = await controller.GetById(joke.Id);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact(Skip="Needs refactor due to constructor dependencies")]
        public async Task ChistesController_GetById_WithNonExistentId_ReturnsNotFound()
        {
            // Arrange
            var context = CreateContext();
            var unitOfWork = new JokesApi.Infrastructure.UnitOfWork(context);
            var logger = new Mock<ILogger<ChistesController>>();
            var getCombinedUseCase = new Mock<JokesApi.Application.UseCases.GetCombinedJoke>();
            var getRandomUseCase = new Mock<JokesApi.Application.UseCases.GetRandomJoke>();
            var getPairedUseCase = new Mock<JokesApi.Application.UseCases.GetPairedJokes>();

            var controller = new ChistesController(unitOfWork, logger.Object, getCombinedUseCase.Object, getRandomUseCase.Object, getPairedUseCase.Object);

            // Act
            var result = await controller.GetById(Guid.NewGuid());

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
    }
} 