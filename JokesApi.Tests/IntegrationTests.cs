using System;
using System.Net;
using System.Net.Http;
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
using System.Collections.Generic;
using System.Linq;

namespace JokesApi.Tests
{
    public class IntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public IntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove existing DbContext registration
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(AppDbContext));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    // Add in-memory database for testing
                    services.AddDbContext<AppDbContext>(options =>
                    {
                        options.UseInMemoryDatabase(Guid.NewGuid().ToString());
                    });
                });
            });

            _client = _factory.CreateClient();
        }

        /*
        [Fact]
        public async Task HealthCheck_ReturnsOk()
        {
            // Act
            var response = await _client.GetAsync("/health");
            
            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Swagger_ReturnsOk()
        {
            // Act
            var response = await _client.GetAsync("/swagger/v1/swagger.json");
            
            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task ChistesController_GetRandom_ReturnsUnauthorized_WhenNoToken()
        {
            // Act
            var response = await _client.GetAsync("/api/chistes/aleatorio");
            
            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task ChistesController_GetPaired_ReturnsUnauthorized_WhenNoToken()
        {
            // Act
            var response = await _client.GetAsync("/api/chistes/emparejados");
            
            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task ChistesController_GetCombined_ReturnsUnauthorized_WhenNoToken()
        {
            // Act
            var response = await _client.GetAsync("/api/chistes/combinado");
            
            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task MathController_Endpoints_ReturnUnauthorized_WhenNoToken()
        {
            // Act & Assert
            var lcmResponse = await _client.GetAsync("/api/matematicas/mcm?numbers=1,2,3");
            var nextResponse = await _client.GetAsync("/api/matematicas/siguiente-numero?number=5");
            var addResponse = await _client.GetAsync("/api/matematicas/add?a=2&b=3");
            var subtractResponse = await _client.GetAsync("/api/matematicas/subtract?a=5&b=2");
            var multiplyResponse = await _client.GetAsync("/api/matematicas/multiply?a=3&b=4");
            
            Assert.Equal(HttpStatusCode.Unauthorized, lcmResponse.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, nextResponse.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, addResponse.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, subtractResponse.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, multiplyResponse.StatusCode);
        }

        [Fact]
        public async Task RateLimiting_Works_ForLoginEndpoint()
        {
            // Act - Make multiple requests to trigger rate limiting
            var requests = new List<Task<HttpResponseMessage>>();
            for (int i = 0; i < 10; i++)
            {
                var loginData = new { Email = $"test{i}@example.com", Password = "password" };
                requests.Add(_client.PostAsJsonAsync("/api/auth/login", loginData));
            }

            var responses = await Task.WhenAll(requests);
            
            // Assert - At least some should be rate limited
            var rateLimitedCount = responses.Count(r => r.StatusCode == HttpStatusCode.TooManyRequests);
            Assert.True(rateLimitedCount > 0, "Rate limiting should be triggered");
        }

        [Fact]
        public async Task ErrorHandling_Returns500_ForUnhandledExceptions()
        {
            // Arrange - Create a request that might cause an exception
            var invalidData = new { InvalidProperty = "data" };
            
            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/login", invalidData);
            
            // Assert - Should handle the error gracefully
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest || 
                       response.StatusCode == HttpStatusCode.InternalServerError,
                       "Should handle errors gracefully");
        }
        */
    }
} 