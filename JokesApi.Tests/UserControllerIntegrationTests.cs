using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace JokesApi.Tests
{
    public class UserControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public UserControllerIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Aquí podríamos reemplazar servicios para testing
                });
            });

            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task GetUsers_ReturnsUnauthorized_WhenNoToken()
        {
            // Act
            var response = await _client.GetAsync("/api/users");
            
            // Assert
            Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Register_ReturnsCreated_WithValidData()
        {
            // Arrange
            var uniqueEmail = $"test_{Guid.NewGuid()}@example.com";
            var registerData = new
            {
                Name = "Test User",
                Email = uniqueEmail,
                Password = "Test123!"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/users/register", registerData);
            
            // Assert
            Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
        }
    }
} 