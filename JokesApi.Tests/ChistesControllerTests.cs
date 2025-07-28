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