using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LegacyOrderApi.DTOs;
using LegacyOrderApi.Models;
using LegacyOrderApi.Repositories;
using LegacyOrderApi.Services;
using LegacyOrderApi.Services.Discounts;
using Moq;
using Xunit;

namespace LegacyOrderApi.Tests
{
    public class OrderServiceTests
    {
        private readonly Mock<IOrderRepository> _mockRepo;
        private readonly OrderService _service;

        public OrderServiceTests()
        {
            _mockRepo = new Mock<IOrderRepository>();
            var discountStrategies = new List<IDiscountStrategy>
            {
                new HighValueDiscountStrategy(),
                new MidValueDiscountStrategy()
            };
            _service = new OrderService(_mockRepo.Object, discountStrategies);
        }

        [Fact]
        public async Task CreateOrderAsync_NullOrEmptyItems_ThrowsArgumentException()
        {
            // Arrange
            var request = new CreateOrderRequest
            {
                Email = "test@test.com",
                Items = new List<OrderItemRequest>()
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateOrderAsync(request));
        }

        [Fact]
        public async Task CreateOrderAsync_ValidItems_AppliesDiscountsCorrectly()
        {
            // Arrange
            var request = new CreateOrderRequest
            {
                Email = "test@test.com",
                Items = new List<OrderItemRequest>
                {
                    new OrderItemRequest { ProductId = 1, Quantity = 2 } // Total = 1200 (should get 10% discount -> 1080)
                }
            };

            _mockRepo.Setup(x => x.GetUserByEmailAsync("test@test.com", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new User { Id = 1, Email = "test@test.com", IsActive = true, AccountBalance = 2000 });

            _mockRepo.Setup(x => x.GetProductsByIdsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Product>
                {
                    new Product { Id = 1, Price = 600m, StockQuantity = 10 }
                });

            // Act
            var result = await _service.CreateOrderAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(1080m, result.Total); // 1200 * 0.9 = 1080
        }

        [Fact]
        public async Task CreateOrderAsync_OutOfStock_ReturnsError()
        {
            // Arrange
            var request = new CreateOrderRequest
            {
                Email = "test@test.com",
                Items = new List<OrderItemRequest>
                {
                    new OrderItemRequest { ProductId = 1, Quantity = 100 }
                }
            };

            _mockRepo.Setup(x => x.GetUserByEmailAsync("test@test.com", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new User { Id = 1, Email = "test@test.com", IsActive = true, AccountBalance = 5000 });

            _mockRepo.Setup(x => x.GetProductsByIdsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Product>
                {
                    new Product { Id = 1, Price = 10m, StockQuantity = 5 } // Only 5 in stock!
                });

            // Act
            var result = await _service.CreateOrderAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("out of stock", result.Status);
        }

        // Test: validation rejects orders with negative quantity
        [Fact]
        public async Task CreateOrderAsync_NegativeQuantity_ReturnsError()
        {
            var request = new CreateOrderRequest
            {
                Email = "test@test.com",
                Items = new List<OrderItemRequest>
                {
                    new OrderItemRequest { ProductId = 1, Quantity = -5 }
                }
            };

            _mockRepo.Setup(x => x.GetUserByEmailAsync("test@test.com", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new User { Id = 1, Email = "test@test.com", IsActive = true, AccountBalance = 5000 });

            _mockRepo.Setup(x => x.GetProductsByIdsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Product>
                {
                    new Product { Id = 1, Name = "Widget", Price = 10m, StockQuantity = 100 }
                });

            var result = await _service.CreateOrderAsync(request);

            Assert.False(result.Success);
            Assert.Contains("Invalid quantity", result.Status);
        }

        // Test: validation rejects orders where user has exactly 0 balance
        [Fact]
        public async Task CreateOrderAsync_ZeroBalance_ReturnsError()
        {
            var request = new CreateOrderRequest
            {
                Email = "test@test.com",
                Items = new List<OrderItemRequest>
                {
                    new OrderItemRequest { ProductId = 1, Quantity = 1 }
                }
            };

            _mockRepo.Setup(x => x.GetUserByEmailAsync("test@test.com", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new User { Id = 1, Email = "test@test.com", IsActive = true, AccountBalance = 0 }); // 0 balance

            _mockRepo.Setup(x => x.GetProductsByIdsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Product>
                {
                    new Product { Id = 1, Name = "Widget", Price = 10m, StockQuantity = 100 }
                });

            var result = await _service.CreateOrderAsync(request);

            Assert.False(result.Success);
            Assert.Contains("Insufficient funds", result.Status);
        }

        // Test: discount strategy applies the highest possible discount
        [Fact]
        public async Task CreateOrderAsync_HighValue_AppliesTenPercentDiscount()
        {
            var request = new CreateOrderRequest
            {
                Email = "test@test.com",
                Items = new List<OrderItemRequest>
                {
                    new OrderItemRequest { ProductId = 1, Quantity = 20 } // Total = 2000
                }
            };

            _mockRepo.Setup(x => x.GetUserByEmailAsync("test@test.com", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new User { Id = 1, Email = "test@test.com", IsActive = true, AccountBalance = 5000 });

            _mockRepo.Setup(x => x.GetProductsByIdsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Product>
                {
                    new Product { Id = 1, Name = "Widget", Price = 100m, StockQuantity = 100 }
                });

            var result = await _service.CreateOrderAsync(request);

            Assert.True(result.Success);
            Assert.Equal(1800m, result.Total); // 2000 - 10% (200) = 1800
        }
    }
}
