using System;
using System.Threading.Tasks;
using JokesApi.Application.Ports;
using JokesApi.Application.UseCases;
using JokesApi.Controllers;
using JokesApi.Data;
using JokesApi.Entities;
using JokesApi.Infrastructure;
using JokesApi.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

namespace JokesApi.Tests
{
    public class EdgeCaseTests
    {
        private AppDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public void MathController_NextNumber_WithMaxInt_ReturnsOverflowedValue()
        {
            // Arrange
            var logger = new Mock<ILogger<MathController>>();
            var controller = new MathController(logger.Object);

            // Act
            var result = controller.NextNumber(int.MaxValue);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var value = okResult.Value;
            var resultProperty = value.GetType().GetProperty("result");
            // In C#, int.MaxValue + 1 = int.MinValue due to overflow
            Assert.Equal(int.MinValue, resultProperty.GetValue(value));
        }

        [Fact]
        public void MathController_Add_WithMaxInt_ReturnsOverflowedValue()
        {
            // Arrange
            var logger = new Mock<ILogger<MathController>>();
            var controller = new MathController(logger.Object);

            // Act
            var result = controller.Add(int.MaxValue, 1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            // In C#, int.MaxValue + 1 = int.MinValue due to overflow
            Assert.Equal(int.MinValue, okResult.Value);
        }

        [Fact]
        public void MathController_Subtract_WithMinInt_ReturnsOverflowedValue()
        {
            // Arrange
            var logger = new Mock<ILogger<MathController>>();
            var controller = new MathController(logger.Object);

            // Act
            var result = controller.Subtract(int.MinValue, 1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            // In C#, int.MinValue - 1 = int.MaxValue due to overflow
            Assert.Equal(int.MaxValue, okResult.Value);
        }

        [Fact]
        public void MathController_Multiply_WithMaxInt_ReturnsOverflowedValue()
        {
            // Arrange
            var logger = new Mock<ILogger<MathController>>();
            var controller = new MathController(logger.Object);

            // Act
            var result = controller.Multiply(int.MaxValue, 2);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            // In C#, int.MaxValue * 2 = -2 due to overflow
            Assert.Equal(-2, okResult.Value);
        }

        [Fact]
        public void MathController_Lcm_WithZero_ReturnsZero()
        {
            // Arrange
            var logger = new Mock<ILogger<MathController>>();
            var controller = new MathController(logger.Object);

            // Act
            var result = controller.Lcm("0,5,10");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var value = okResult.Value;
            var resultProperty = value.GetType().GetProperty("lcm");
            Assert.Equal(0L, resultProperty.GetValue(value));
        }

        [Fact]
        public void MathController_Lcm_WithSingleNumber_ReturnsNumber()
        {
            // Arrange
            var logger = new Mock<ILogger<MathController>>();
            var controller = new MathController(logger.Object);

            // Act
            var result = controller.Lcm("12");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var value = okResult.Value;
            var resultProperty = value.GetType().GetProperty("lcm");
            Assert.Equal(12L, resultProperty.GetValue(value));
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
        public void MathController_Lcm_WithNullString_ReturnsBadRequest()
        {
            // Arrange
            var logger = new Mock<ILogger<MathController>>();
            var controller = new MathController(logger.Object);

            // Act
            var result = controller.Lcm(null);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public void MathController_Lcm_WithInvalidFormat_ReturnsBadRequest()
        {
            // Arrange
            var logger = new Mock<ILogger<MathController>>();
            var controller = new MathController(logger.Object);

            // Act
            var result = controller.Lcm("1,2,a,4");

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task AuthController_Login_WithNullRequest_ReturnsBadRequest()
        {
            // Arrange
            var context = CreateContext();
            var tokenService = new Mock<JokesApi.Services.ITokenService>();
            var logger = new Mock<ILogger<AuthController>>();
            var controller = new AuthController(context, tokenService.Object, logger.Object);

            // Act
            var result = await controller.Login(null);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task AuthController_Login_WithEmptyEmail_ReturnsUnauthorized()
        {
            // Arrange
            var context = CreateContext();
            var tokenService = new Mock<JokesApi.Services.ITokenService>();
            var logger = new Mock<ILogger<AuthController>>();
            var controller = new AuthController(context, tokenService.Object, logger.Object);
            var request = new AuthController.LoginRequest("", "password");

            // Act
            var result = await controller.Login(request);

            // Assert
            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task AuthController_Login_WithEmptyPassword_ReturnsUnauthorized()
        {
            // Arrange
            var context = CreateContext();
            var tokenService = new Mock<JokesApi.Services.ITokenService>();
            var logger = new Mock<ILogger<AuthController>>();
            var controller = new AuthController(context, tokenService.Object, logger.Object);
            var request = new AuthController.LoginRequest("test@test.com", "");

            // Act
            var result = await controller.Login(request);

            // Assert
            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public void AuthController_Refresh_WithNullRequest_ReturnsUnauthorized()
        {
            // Arrange
            var context = CreateContext();
            var tokenService = new Mock<JokesApi.Services.ITokenService>();
            var logger = new Mock<ILogger<AuthController>>();
            var controller = new AuthController(context, tokenService.Object, logger.Object);

            // Act
            var result = controller.Refresh(null);

            // Assert
            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public void AuthController_Refresh_WithEmptyToken_ReturnsUnauthorized()
        {
            // Arrange
            var context = CreateContext();
            var tokenService = new Mock<JokesApi.Services.ITokenService>();
            var logger = new Mock<ILogger<AuthController>>();
            var controller = new AuthController(context, tokenService.Object, logger.Object);
            var request = new AuthController.RefreshRequest("");

            // Act
            var result = controller.Refresh(request);

            // Assert
            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public void AuthController_Revoke_WithNullRequest_ReturnsNoContent()
        {
            // Arrange
            var context = CreateContext();
            var tokenService = new Mock<JokesApi.Services.ITokenService>();
            var logger = new Mock<ILogger<AuthController>>();
            var controller = new AuthController(context, tokenService.Object, logger.Object);

            // Act
            var result = controller.Revoke(null);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public void AuthController_Revoke_WithEmptyToken_ReturnsNoContent()
        {
            // Arrange
            var context = CreateContext();
            var tokenService = new Mock<JokesApi.Services.ITokenService>();
            var logger = new Mock<ILogger<AuthController>>();
            var controller = new AuthController(context, tokenService.Object, logger.Object);
            var request = new AuthController.RevokeRequest("");

            // Act
            var result = controller.Revoke(request);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task GetRandomJoke_WithNullOrigin_ReturnsJoke()
        {
            // Arrange
            var chuckClient = new Mock<IChuckClient>();
            var dadClient = new Mock<IDadClient>();
            chuckClient.Setup(x => x.GetRandomJokeAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync("Chuck joke");
            dadClient.Setup(x => x.GetRandomJokeAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync("Dad joke");

            var useCase = new GetRandomJoke(chuckClient.Object, dadClient.Object);

            // Act
            var result = await useCase.ExecuteAsync(null);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        [Fact]
        public async Task GetRandomJoke_WithChuckOrigin_ReturnsChuckJoke()
        {
            // Arrange
            var chuckClient = new Mock<IChuckClient>();
            var dadClient = new Mock<IDadClient>();
            chuckClient.Setup(x => x.GetRandomJokeAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync("Chuck joke");
            dadClient.Setup(x => x.GetRandomJokeAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync("Dad joke");

            var useCase = new GetRandomJoke(chuckClient.Object, dadClient.Object);

            // Act
            var result = await useCase.ExecuteAsync("chuck");

            // Assert
            Assert.Equal("Chuck joke", result);
        }

        [Fact]
        public async Task GetPairedJokes_WithValidCount_ReturnsPairedJokes()
        {
            // Arrange
            var chuckClient = new Mock<IChuckClient>();
            var dadClient = new Mock<IDadClient>();
            chuckClient.Setup(x => x.GetRandomJokeAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync("Chuck joke");
            dadClient.Setup(x => x.GetRandomJokeAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync("Dad joke");

            var useCase = new GetPairedJokes(chuckClient.Object, dadClient.Object);

            // Act
            var result = await useCase.ExecuteAsync(2);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.All(result, item => Assert.NotNull(item.Combinado));
        }

        [Fact]
        public async Task GetCombinedJoke_WithValidData_ReturnsCombinedJoke()
        {
            // Arrange
            var chuckClient = new Mock<IChuckClient>();
            var dadClient = new Mock<IDadClient>();
            var unitOfWork = new Mock<IUnitOfWork>();
            var jokeRepository = new Mock<IJokeRepository>();
            
            chuckClient.Setup(x => x.GetRandomJokeAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync("Chuck joke.");
            dadClient.Setup(x => x.GetRandomJokeAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync("Dad joke.");
            unitOfWork.Setup(x => x.Jokes).Returns(jokeRepository.Object);
            
            // Use a real DbContext with in-memory database for this test
            var context = CreateContext();
            var joke = new Joke { Id = Guid.NewGuid(), Text = "Local joke." };
            context.Jokes.Add(joke);
            await context.SaveChangesAsync();
            
            // Mock the repository to return the real context's queryable
            jokeRepository.Setup(x => x.Query).Returns(context.Jokes.AsQueryable());

            var useCase = new GetCombinedJoke(chuckClient.Object, dadClient.Object, unitOfWork.Object);

            // Act
            var result = await useCase.ExecuteAsync();

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        [Fact]
        public async Task GetCombinedJoke_WithNoJokesAvailable_ThrowsException()
        {
            // Arrange
            var chuckClient = new Mock<IChuckClient>();
            var dadClient = new Mock<IDadClient>();
            var unitOfWork = new Mock<IUnitOfWork>();
            var jokeRepository = new Mock<IJokeRepository>();
            
            chuckClient.Setup(x => x.GetRandomJokeAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((string)null);
            dadClient.Setup(x => x.GetRandomJokeAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((string)null);
            unitOfWork.Setup(x => x.Jokes).Returns(jokeRepository.Object);
            
            // Mock empty list
            jokeRepository.Setup(x => x.Query).Returns(new List<Joke>().AsQueryable());

            var useCase = new GetCombinedJoke(chuckClient.Object, dadClient.Object, unitOfWork.Object);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.ExecuteAsync());
        }
    }
} 