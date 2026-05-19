using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using LegacyOrderApi.DTOs;
using LegacyOrderApi.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LegacyOrderApi.Tests
{
    public class OrderControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public OrderControllerIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Seed the database here
                    var sp = services.BuildServiceProvider();
                    using var scope = sp.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    db.Database.EnsureCreated();

                    if (!db.Users.Any())
                    {
                        db.Users.Add(new User { Id = 1, Email = "integration@test.com", IsActive = true, AccountBalance = 1000m });
                        db.Products.Add(new Product { Id = 1, Name = "Widget", Price = 100m, StockQuantity = 50 });
                        db.SaveChanges();
                    }
                });
            }).CreateClient();
        }

        [Fact]
        public async Task Post_ValidOrder_ReturnsSuccess()
        {
            // Arrange
            var request = new CreateOrderRequest
            {
                Email = "integration@test.com",
                ShippingAddress = "123 Test St",
                Items = new List<OrderItemRequest>
                {
                    new OrderItemRequest { ProductId = 1, Quantity = 2 } // Total = 200
                }
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/order", request); // Route is api/order (from api/[controller])

            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<OrderResponse>();
            
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal(200m, result.Total); // No discount for 200
        }

        [Fact]
        public async Task Post_InsufficientFunds_ReturnsBadRequest()
        {
            // Arrange
            var request = new CreateOrderRequest
            {
                Email = "integration@test.com",
                ShippingAddress = "123 Test St",
                Items = new List<OrderItemRequest>
                {
                    new OrderItemRequest { ProductId = 1, Quantity = 50 } // Total = 5000 (discount -> 4500, still > 1000 balance)
                }
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/order", request);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Contains("Insufficient funds", result.Message);
        }
    }
}
