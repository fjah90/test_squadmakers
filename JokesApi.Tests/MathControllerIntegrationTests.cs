using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using JokesApi.Data;
using JokesApi.Entities;
using JokesApi.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace JokesApi.Tests
{
    public class MathControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private readonly string _token;

        public MathControllerIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Aquí podríamos reemplazar servicios para testing
                });
            });

            _client = _factory.CreateClient();
            
            // Crear un token JWT válido para las pruebas
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var db = new AppDbContext(options);
            
            var jwtSettings = Options.Create(new JokesApi.Settings.JwtSettings 
            { 
                Key = "super-secret-key-for-testing-at-least-32-chars",
                Issuer = "test-issuer",
                Audience = "test-audience",
                ExpirationMinutes = 60
            });
            
            var tokenService = new TokenService(db, jwtSettings);
            
            var user = new User
            {
                Id = Guid.NewGuid(),
                Name = "Test User",
                Email = "test@example.com",
                PasswordHash = "not-needed-for-token-generation",
                Role = "user"
            };
            
            _token = tokenService.CreateTokenPair(user).Token;
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        }

        [Fact]
        public async Task Add_ReturnsCorrectSum()
        {
            // Arrange
            var a = 5;
            var b = 3;

            // Act
            var response = await _client.GetAsync($"/api/math/add?a={a}&b={b}");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<int>();

            // Assert
            Assert.Equal(8, result);
        }

        [Fact]
        public async Task Subtract_ReturnsCorrectDifference()
        {
            // Arrange
            var a = 5;
            var b = 3;

            // Act
            var response = await _client.GetAsync($"/api/math/subtract?a={a}&b={b}");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<int>();

            // Assert
            Assert.Equal(2, result);
        }

        [Fact]
        public async Task Multiply_ReturnsCorrectProduct()
        {
            // Arrange
            var a = 5;
            var b = 3;

            // Act
            var response = await _client.GetAsync($"/api/math/multiply?a={a}&b={b}");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<int>();

            // Assert
            Assert.Equal(15, result);
        }
    }
} 