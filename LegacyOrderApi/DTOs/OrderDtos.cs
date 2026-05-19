using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LegacyOrderApi.DTOs
{
    public class CreateOrderRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        
        public string ShippingAddress { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "At least one item is required.")]
        public List<OrderItemRequest> Items { get; set; }
    }

    public class OrderItemRequest
    {
        [Range(1, int.MaxValue)]
        public int ProductId { get; set; }
        
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
    }

    public class OrderResponse
    {
        public bool Success { get; set; }
        public int OrderId { get; set; }
        public decimal Total { get; set; }
        public string Status { get; set; }
    }

    public class ErrorResponse
    {
        public bool Success { get; set; } = false;
        public string Message { get; set; }
    }
}
