using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LegacyOrderApi.DTOs;
using LegacyOrderApi.Models;
using LegacyOrderApi.Repositories;
using LegacyOrderApi.Services.Discounts;

namespace LegacyOrderApi.Services
{
    public interface IOrderService
    {
        Task<OrderResponse> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken = default);
    }

    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _repository;
        private readonly IEnumerable<IDiscountStrategy> _discountStrategies;

        public OrderService(IOrderRepository repository, IEnumerable<IDiscountStrategy> discountStrategies)
        {
            _repository = repository;
            _discountStrategies = discountStrategies;
        }

        public async Task<OrderResponse> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            
            if (request.Items == null || !request.Items.Any())
                throw new ArgumentException("No items provided in the order request.", nameof(request));

            // Fetch user
            var user = await _repository.GetUserByEmailAsync(request.Email, cancellationToken);
            if (user == null)
            {
                // Smell 8 fix: Do not create user implicitly. Fail the request.
                return new OrderResponse { Success = false, Status = "User not found." };
            }

            if (!user.IsActive)
            {
                return new OrderResponse { Success = false, Status = "User inactive." };
            }

            // Smell 7 fix: N+1 query anti-pattern resolved by batch fetching products
            var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
            var products = await _repository.GetProductsByIdsAsync(productIds, cancellationToken);
            
            var productDictionary = products.ToDictionary(p => p.Id);
            
            decimal totalAmount = 0;
            var orderItems = new List<OrderItem>();

            // Smell 5 fix: Use foreach instead of buggy for-loop (off-by-one)
            foreach (var itemReq in request.Items)
            {
                if (productDictionary.TryGetValue(itemReq.ProductId, out var product))
                {
                    if (product.StockQuantity >= itemReq.Quantity)
                    {
                        totalAmount += product.Price * itemReq.Quantity;
                        
                        product.StockQuantity -= itemReq.Quantity;
                        await _repository.UpdateProductAsync(product, cancellationToken);
                        
                        orderItems.Add(new OrderItem
                        {
                            ProductId = product.Id,
                            Quantity = itemReq.Quantity,
                            UnitPrice = product.Price
                        });
                    }
                    else
                    {
                        // Specific business logic rule: fail if out of stock
                        return new OrderResponse { Success = false, Status = $"Product {product.Name} is out of stock." };
                    }
                }
                else
                {
                    return new OrderResponse { Success = false, Status = $"Product with ID {itemReq.ProductId} not found." };
                }
            }

            if (!orderItems.Any())
            {
                return new OrderResponse { Success = false, Status = "No valid items to order." };
            }

            // Smell 10 fix: Apply discount policy via Strategy Pattern
            var applicableStrategy = _discountStrategies.FirstOrDefault(s => s.IsApplicable(totalAmount));
            if (applicableStrategy != null)
            {
                totalAmount = applicableStrategy.ApplyDiscount(totalAmount);
            }

            // Smell 11 fix: Prevent negative balance
            if (user.AccountBalance < totalAmount)
            {
                return new OrderResponse { Success = false, Status = "Insufficient funds." };
            }

            user.AccountBalance -= totalAmount;
            await _repository.UpdateUserAsync(user, cancellationToken);

            var order = new Order
            {
                UserId = user.Id,
                OrderDate = DateTime.UtcNow,
                TotalAmount = totalAmount,
                Status = "Completed", // Or "Pending" based on requirements
                ShippingAddress = request.ShippingAddress ?? "Unknown",
                Items = orderItems
            };

            await _repository.AddOrderAsync(order, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);

            return new OrderResponse
            {
                Success = true,
                OrderId = order.Id,
                Total = totalAmount,
                Status = order.Status
            };
        }


    }
}
