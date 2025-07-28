using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace JokesApi.Tests
{
    public class MathControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

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