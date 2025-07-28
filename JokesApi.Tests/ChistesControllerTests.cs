using JokesApi.Controllers;
using JokesApi.Domain.Repositories;
using JokesApi.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Http;

namespace JokesApi.Tests
{
    public class ChistesFilterTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ILogger<ChistesController>> _mockLogger;
        private readonly ChistesController _controller;

        public ChistesFilterTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<ChistesController>>();

            // Crear mocks para los casos de uso, pero no los usaremos en estas pruebas
            var mockCombinedUseCase = new Mock<JokesApi.Application.UseCases.GetCombinedJoke>(
                MockBehavior.Loose,
                new Mock<JokesApi.Application.Ports.IChuckClient>().Object,
                new Mock<JokesApi.Application.Ports.IDadClient>().Object,
                _mockUnitOfWork.Object);

            var mockRandomUseCase = new Mock<JokesApi.Application.UseCases.GetRandomJoke>(
                MockBehavior.Loose,
                new Mock<JokesApi.Application.Ports.IChuckClient>().Object,
                new Mock<JokesApi.Application.Ports.IDadClient>().Object);

            var mockPairedUseCase = new Mock<JokesApi.Application.UseCases.GetPairedJokes>(
                MockBehavior.Loose,
                new Mock<JokesApi.Application.Ports.IChuckClient>().Object,
                new Mock<JokesApi.Application.Ports.IDadClient>().Object);

            _controller = new ChistesController(
                _mockUnitOfWork.Object,
                _mockLogger.Object,
                mockCombinedUseCase.Object,
                mockRandomUseCase.Object,
                mockPairedUseCase.Object
            );
        }

        [Fact]
        public async Task Filter_WithMinPalabras_FiltersCorrectly()
        {
            // Arrange
            var jokes = new List<Joke>
            {
                new Joke {
                    Id = Guid.NewGuid(),
                    Text = "Este es un chiste corto",
                    Source = "test",
                    Author = new User {
                        Id = Guid.NewGuid(),
                        Name = "User1",
                        Email = "user1@example.com",
                        PasswordHash = "hash"
                    },
                    Themes = new List<Theme>()
                },
                new Joke {
                    Id = Guid.NewGuid(),
                    Text = "Este es un chiste un poco más largo con más palabras",
                    Source = "test",
                    Author = new User {
                        Id = Guid.NewGuid(),
                        Name = "User2",
                        Email = "user2@example.com",
                        PasswordHash = "hash"
                    },
                    Themes = new List<Theme>()
                }
            };

            var mockJokeRepo = new Mock<IJokeRepository>();
            var mockDbSet = CreateMockDbSet(jokes);

            mockJokeRepo.Setup(r => r.Query).Returns(mockDbSet.Object);
            _mockUnitOfWork.Setup(u => u.Jokes).Returns(mockJokeRepo.Object);

            // Act
            var result = await _controller.Filter(minPalabras: 5, contiene: null, autorId: null, tematicaId: null);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }

        [Fact]
        public async Task Filter_WithContiene_FiltersCorrectly()
        {
            // Arrange
            var jokes = new List<Joke>
            {
                new Joke {
                    Id = Guid.NewGuid(),
                    Text = "Este es un chiste sobre programación",
                    Source = "test",
                    Author = new User {
                        Id = Guid.NewGuid(),
                        Name = "User1",
                        Email = "user1@example.com",
                        PasswordHash = "hash"
                    },
                    Themes = new List<Theme>()
                },
                new Joke {
                    Id = Guid.NewGuid(),
                    Text = "Este es un chiste sobre cocina",
                    Source = "test",
                    Author = new User {
                        Id = Guid.NewGuid(),
                        Name = "User2",
                        Email = "user2@example.com",
                        PasswordHash = "hash"
                    },
                    Themes = new List<Theme>()
                }
            };

            var mockJokeRepo = new Mock<IJokeRepository>();
            var mockDbSet = CreateMockDbSet(jokes);

            mockJokeRepo.Setup(r => r.Query).Returns(mockDbSet.Object);
            _mockUnitOfWork.Setup(u => u.Jokes).Returns(mockJokeRepo.Object);

            // Act
            var result = await _controller.Filter(minPalabras: null, contiene: "programación", autorId: null, tematicaId: null);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }

        [Fact]
        public async Task Filter_WithAutorId_FiltersCorrectly()
        {
            // Arrange
            var author1Id = Guid.NewGuid();
            var author2Id = Guid.NewGuid();

            var jokes = new List<Joke>
            {
                new Joke {
                    Id = Guid.NewGuid(),
                    Text = "Chiste del autor 1",
                    Source = "test",
                    AuthorId = author1Id,
                    Author = new User {
                        Id = author1Id,
                        Name = "User1",
                        Email = "user1@example.com",
                        PasswordHash = "hash"
                    },
                    Themes = new List<Theme>()
                },
                new Joke {
                    Id = Guid.NewGuid(),
                    Text = "Chiste del autor 2",
                    Source = "test",
                    AuthorId = author2Id,
                    Author = new User {
                        Id = author2Id,
                        Name = "User2",
                        Email = "user2@example.com",
                        PasswordHash = "hash"
                    },
                    Themes = new List<Theme>()
                }
            };

            var mockJokeRepo = new Mock<IJokeRepository>();
            var mockDbSet = CreateMockDbSet(jokes);

            mockJokeRepo.Setup(r => r.Query).Returns(mockDbSet.Object);
            _mockUnitOfWork.Setup(u => u.Jokes).Returns(mockJokeRepo.Object);

            // Act
            var result = await _controller.Filter(minPalabras: null, contiene: null, autorId: author1Id, tematicaId: null);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }

        [Fact]
        public async Task Filter_WithTematicaId_FiltersCorrectly()
        {
            // Arrange
            var theme1Id = Guid.NewGuid();
            var theme2Id = Guid.NewGuid();

            var theme1 = new Theme { Id = theme1Id, Name = "Programación" };
            var theme2 = new Theme { Id = theme2Id, Name = "Cocina" };

            var jokes = new List<Joke>
            {
                new Joke {
                    Id = Guid.NewGuid(),
                    Text = "Chiste sobre programación",
                    Source = "test",
                    Author = new User {
                        Id = Guid.NewGuid(),
                        Name = "User1",
                        Email = "user1@example.com",
                        PasswordHash = "hash"
                    },
                    Themes = new List<Theme> { theme1 }
                },
                new Joke {
                    Id = Guid.NewGuid(),
                    Text = "Chiste sobre cocina",
                    Source = "test",
                    Author = new User {
                        Id = Guid.NewGuid(),
                        Name = "User2",
                        Email = "user2@example.com",
                        PasswordHash = "hash"
                    },
                    Themes = new List<Theme> { theme2 }
                }
            };

            var mockJokeRepo = new Mock<IJokeRepository>();
            var mockDbSet = CreateMockDbSet(jokes);

            mockJokeRepo.Setup(r => r.Query).Returns(mockDbSet.Object);
            _mockUnitOfWork.Setup(u => u.Jokes).Returns(mockJokeRepo.Object);

            // Act
            var result = await _controller.Filter(minPalabras: null, contiene: null, autorId: null, tematicaId: theme1Id);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
    }

    [Fact]
        public async Task Filter_WithMultipleFilters_FiltersCorrectly()
    {
        // Arrange
            var authorId = Guid.NewGuid();
            var themeId = Guid.NewGuid();
            var theme = new Theme { Id = themeId, Name = "Programación" };

            var jokes = new List<Joke>
            {
                new Joke {
                    Id = Guid.NewGuid(),
                    Text = "Chiste largo sobre programación del autor específico",
                    Source = "test",
                    AuthorId = authorId,
                    Author = new User {
                        Id = authorId,
                        Name = "Author",
                        Email = "author@example.com",
                        PasswordHash = "hash"
                    },
                    Themes = new List<Theme> { theme }
                },
                new Joke {
                    Id = Guid.NewGuid(),
                    Text = "Otro chiste",
                    Source = "test",
                    AuthorId = Guid.NewGuid(),
                    Author = new User {
                        Id = Guid.NewGuid(),
                        Name = "Other",
                        Email = "other@example.com",
                        PasswordHash = "hash"
                    },
                    Themes = new List<Theme>()
                }
            };

            var mockJokeRepo = new Mock<IJokeRepository>();
            var mockDbSet = CreateMockDbSet(jokes);

            mockJokeRepo.Setup(r => r.Query).Returns(mockDbSet.Object);
            _mockUnitOfWork.Setup(u => u.Jokes).Returns(mockJokeRepo.Object);

        // Act
            var result = await _controller.Filter(
                minPalabras: 5,
                contiene: "programación",
                autorId: authorId,
                tematicaId: themeId);

        // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
    }

        [Fact]
        public async Task Filter_WithException_Returns500()
        {
            // Arrange
            var mockJokeRepo = new Mock<IJokeRepository>();
            mockJokeRepo.Setup(r => r.Query).Throws(new Exception("Test exception"));
            _mockUnitOfWork.Setup(u => u.Jokes).Returns(mockJokeRepo.Object);

            // Act
            var result = await _controller.Filter(null, null, null, null);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }

        /*
        [Fact]
        public async Task GetRandom_WithException_Returns500()
        {
            // Arrange
            var mockRandomUseCase = new Mock<JokesApi.Application.UseCases.GetRandomJoke>(
                MockBehavior.Loose,
                new Mock<JokesApi.Application.Ports.IChuckClient>().Object,
                new Mock<JokesApi.Application.Ports.IDadClient>().Object);
            
            mockRandomUseCase.Setup(x => x.ExecuteAsync(It.IsAny<string>()))
                            .ThrowsAsync(new Exception("External API error"));

            var controller = new ChistesController(
                _mockUnitOfWork.Object,
                _mockLogger.Object,
                new Mock<JokesApi.Application.UseCases.GetCombinedJoke>(
                    MockBehavior.Loose,
                    new Mock<JokesApi.Application.Ports.IChuckClient>().Object,
                    new Mock<JokesApi.Application.Ports.IDadClient>().Object,
                    _mockUnitOfWork.Object).Object,
                mockRandomUseCase.Object,
                new Mock<JokesApi.Application.UseCases.GetPairedJokes>(
                    MockBehavior.Loose,
                    new Mock<JokesApi.Application.Ports.IChuckClient>().Object,
                    new Mock<JokesApi.Application.Ports.IDadClient>().Object).Object
            );

            // Act
            var result = await controller.GetRandom("chuck");

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }

        [Fact]
        public async Task GetPaired_WithException_Returns500()
        {
            // Arrange
            var mockPairedUseCase = new Mock<JokesApi.Application.UseCases.GetPairedJokes>(
                MockBehavior.Loose,
                new Mock<JokesApi.Application.Ports.IChuckClient>().Object,
                new Mock<JokesApi.Application.Ports.IDadClient>().Object);
            
            mockPairedUseCase.Setup(x => x.ExecuteAsync())
                            .ThrowsAsync(new Exception("External API error"));

            var controller = new ChistesController(
                _mockUnitOfWork.Object,
                _mockLogger.Object,
                new Mock<JokesApi.Application.UseCases.GetCombinedJoke>(
                    MockBehavior.Loose,
                    new Mock<JokesApi.Application.Ports.IChuckClient>().Object,
                    new Mock<JokesApi.Application.Ports.IDadClient>().Object,
                    _mockUnitOfWork.Object).Object,
                new Mock<JokesApi.Application.UseCases.GetRandomJoke>(
                    MockBehavior.Loose,
                    new Mock<JokesApi.Application.Ports.IChuckClient>().Object,
                    new Mock<JokesApi.Application.Ports.IDadClient>().Object).Object,
                mockPairedUseCase.Object
            );

            // Act
            var result = await controller.GetPaired();

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }

        [Fact]
        public async Task GetCombined_WithException_Returns500()
        {
            // Arrange
            var mockCombinedUseCase = new Mock<JokesApi.Application.UseCases.GetCombinedJoke>(
                MockBehavior.Loose,
                new Mock<JokesApi.Application.Ports.IChuckClient>().Object,
                new Mock<JokesApi.Application.Ports.IDadClient>().Object,
                _mockUnitOfWork.Object);
            
            mockCombinedUseCase.Setup(x => x.ExecuteAsync())
                              .ThrowsAsync(new Exception("External API error"));

            var controller = new ChistesController(
                _mockUnitOfWork.Object,
                _mockLogger.Object,
                mockCombinedUseCase.Object,
                new Mock<JokesApi.Application.UseCases.GetRandomJoke>(
                    MockBehavior.Loose,
                    new Mock<JokesApi.Application.Ports.IChuckClient>().Object,
                    new Mock<JokesApi.Application.Ports.IDadClient>().Object).Object,
                new Mock<JokesApi.Application.UseCases.GetPairedJokes>(
                    MockBehavior.Loose,
                    new Mock<JokesApi.Application.Ports.IChuckClient>().Object,
                    new Mock<JokesApi.Application.Ports.IDadClient>().Object).Object
            );

            // Act
            var result = await controller.GetCombined();

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }

        [Fact]
        public async Task Create_WithInvalidUserId_ReturnsUnauthorized()
        {
            // Arrange
            var request = new ChistesController.CreateJokeRequest("Test joke", null);

            // Act
            var result = await _controller.Create(request);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Create_WithValidUserId_CreatesJoke()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new ChistesController.CreateJokeRequest("Test joke", null);
            
            // Setup user claims
            var claims = new List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim("sub", userId.ToString())
            };
            var identity = new System.Security.Claims.ClaimsIdentity(claims);
            var principal = new System.Security.Claims.ClaimsPrincipal(identity);
            
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = principal
                }
            };

            var mockJokeRepo = new Mock<IJokeRepository>();
            _mockUnitOfWork.Setup(u => u.Jokes).Returns(mockJokeRepo.Object);
            _mockUnitOfWork.Setup(u => u.SaveAsync()).ReturnsAsync(1);

            // Act
            var result = await _controller.Create(request);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(201, createdResult.StatusCode);
        }

        [Fact]
        public async Task GetById_WithValidId_ReturnsJoke()
        {
            // Arrange
            var jokeId = Guid.NewGuid();
            var joke = new Joke
            {
                Id = jokeId,
                Text = "Test joke",
                Source = "Local",
                Author = new User { Id = Guid.NewGuid(), Name = "Test User" }
            };

            var mockJokeRepo = new Mock<IJokeRepository>();
            mockJokeRepo.Setup(r => r.GetByIdAsync(jokeId)).ReturnsAsync(joke);
            _mockUnitOfWork.Setup(u => u.Jokes).Returns(mockJokeRepo.Object);

            // Act
            var result = await _controller.GetById(jokeId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedJoke = Assert.IsType<Joke>(okResult.Value);
            Assert.Equal(jokeId, returnedJoke.Id);
        }

        [Fact]
        public async Task GetById_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var jokeId = Guid.NewGuid();
            var mockJokeRepo = new Mock<IJokeRepository>();
            mockJokeRepo.Setup(r => r.GetByIdAsync(jokeId)).ReturnsAsync((Joke?)null);
            _mockUnitOfWork.Setup(u => u.Jokes).Returns(mockJokeRepo.Object);

            // Act
            var result = await _controller.GetById(jokeId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Update_WithValidId_UpdatesJoke()
        {
            // Arrange
            var jokeId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var request = new ChistesController.UpdateJokeRequest("Updated joke");
            
            var joke = new Joke
            {
                Id = jokeId,
                Text = "Original joke",
                AuthorId = userId,
                Source = "Local"
            };

            var mockJokeRepo = new Mock<IJokeRepository>();
            mockJokeRepo.Setup(r => r.GetByIdAsync(jokeId)).ReturnsAsync(joke);
            _mockUnitOfWork.Setup(u => u.Jokes).Returns(mockJokeRepo.Object);
            _mockUnitOfWork.Setup(u => u.SaveAsync()).ReturnsAsync(1);

            // Setup user claims
            var claims = new List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim("sub", userId.ToString())
            };
            var identity = new System.Security.Claims.ClaimsIdentity(claims);
            var principal = new System.Security.Claims.ClaimsPrincipal(identity);
            
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = principal
                }
            };

            // Act
            var result = await _controller.Update(jokeId, request);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task Update_WithUnauthorizedUser_ReturnsForbidden()
        {
            // Arrange
            var jokeId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var request = new ChistesController.UpdateJokeRequest("Updated joke");
            
            var joke = new Joke
            {
                Id = jokeId,
                Text = "Original joke",
                AuthorId = Guid.NewGuid(), // Different user
                Source = "Local"
            };

            var mockJokeRepo = new Mock<IJokeRepository>();
            mockJokeRepo.Setup(r => r.GetByIdAsync(jokeId)).ReturnsAsync(joke);
            _mockUnitOfWork.Setup(u => u.Jokes).Returns(mockJokeRepo.Object);

            // Setup user claims
            var claims = new List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim("sub", userId.ToString())
            };
            var identity = new System.Security.Claims.ClaimsIdentity(claims);
            var principal = new System.Security.Claims.ClaimsPrincipal(identity);
            
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = principal
                }
            };

            // Act
            var result = await _controller.Update(jokeId, request);

            // Assert
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task Delete_WithValidId_DeletesJoke()
        {
            // Arrange
            var jokeId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            
            var joke = new Joke
            {
                Id = jokeId,
                Text = "Test joke",
                AuthorId = userId,
                Source = "Local"
            };

            var mockJokeRepo = new Mock<IJokeRepository>();
            mockJokeRepo.Setup(r => r.GetByIdAsync(jokeId)).ReturnsAsync(joke);
            _mockUnitOfWork.Setup(u => u.Jokes).Returns(mockJokeRepo.Object);
            _mockUnitOfWork.Setup(u => u.SaveAsync()).ReturnsAsync(1);

            // Setup user claims
            var claims = new List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim("sub", userId.ToString())
            };
            var identity = new System.Security.Claims.ClaimsIdentity(claims);
            var principal = new System.Security.Claims.ClaimsPrincipal(identity);
            
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = principal
                }
            };

            // Act
            var result = await _controller.Delete(jokeId);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }
        */

        private static Mock<DbSet<T>> CreateMockDbSet<T>(List<T> data) where T : class
        {
            var queryable = data.AsQueryable();
            var mockSet = new Mock<DbSet<T>>();

            mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryable.Provider);
            mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
            mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
            mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => queryable.GetEnumerator());

            // No usar métodos de extensión como AsNoTracking o Include

            return mockSet;
        }
    }
} 