using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using LegacyOrderApi.Models;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;

namespace LegacyOrderApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly AppDbContext _db;

        public OrderController(AppDbContext db)
        {
            _db = db;
        }

        // God Method: Mixes everything!
        [HttpPost]
        public async Task<object> Post([FromBody] CreateOrderRequest request)
        {
            // 1. Validation (inline, manual, and buggy)
            if (request == null)
            {
                return new { success = false, message = "Request is null" };
            }

            if (string.IsNullOrEmpty(request.Email))
            {
                return new { success = false, message = "Email required" };
            }

            if (!request.Email.Contains("@"))
            {
                return new { success = false, message = "Invalid email" };
            }
            
            // Bug: Potential null deref here if request.Items is null
            if (request.Items.Count == 0)
            {
                return new { success = false, message = "No items" };
            }

            // 2. Data Access & Business Logic (Synchronous calls in async method)
            // SMELL: Sync over async
            var user = _db.Users.FirstOrDefault(u => u.Email == request.Email);
            
            if (user == null)
            {
                // Creating a user implicitly if not found - bad practice to mix side effects
                user = new User
                {
                    Email = request.Email,
                    IsActive = true,
                    AccountBalance = 0
                };
                _db.Users.Add(user);
                _db.SaveChanges(); // SMELL: Synchronous save
            }

            if (!user.IsActive)
            {
                return new { success = false, message = "User inactive" };
            }

            decimal totalAmount = 0;
            var orderItems = new List<OrderItem>();
            
            // SMELL: Off-by-one bug here! (i <= request.Items.Count)
            for (int i = 0; i <= request.Items.Count; i++)
            {
                try
                {
                    var itemReq = request.Items[i];
                    var product = _db.Products.Find(itemReq.ProductId); // SMELL: Sync DB call inside loop

                    if (product != null)
                    {
                        if (product.StockQuantity >= itemReq.Quantity)
                        {
                            totalAmount += product.Price * itemReq.Quantity;
                            
                            // Decrease stock
                            product.StockQuantity -= itemReq.Quantity;
                            
                            orderItems.Add(new OrderItem
                            {
                                ProductId = product.Id,
                                Quantity = itemReq.Quantity,
                                UnitPrice = product.Price
                            });
                        }
                        else
                        {
                            // Ignore items that are out of stock (Business logic smell - should probably error out)
                        }
                    }
                }
                catch (Exception ex)
                {
                    // SMELL: Empty catch swallowing exceptions (IndexOutOfRangeException will be swallowed here due to off-by-one)
                }
            }

            if (orderItems.Count == 0)
            {
                return new { success = false, message = "No valid items to order" };
            }

            // Apply some random discount logic inline
            if (totalAmount > 1000)
            {
                totalAmount = totalAmount * 0.9m; // 10% discount
            }
            else if (totalAmount > 500)
            {
                totalAmount = totalAmount * 0.95m; // 5% discount
            }

            // Charge user
            if (user.AccountBalance >= totalAmount)
            {
                user.AccountBalance -= totalAmount;
            }
            else
            {
                // If they don't have balance, we just let it go negative? (Business logic flaw)
                user.AccountBalance -= totalAmount;
            }

            var order = new Order
            {
                UserId = user.Id,
                OrderDate = DateTime.Now,
                TotalAmount = totalAmount,
                Status = "Pending",
                ShippingAddress = request.ShippingAddress ?? "Unknown",
                Items = orderItems
            };

            _db.Orders.Add(order);
            
            try
            {
                _db.SaveChanges(); // SMELL: Sync save again
            }
            catch (DbUpdateException)
            {
                // SMELL: Empty catch block
            }

            // Send Email Notification Inline
            try
            {
                var smtpClient = new SmtpClient("smtp.example.com")
                {
                    Port = 587,
                    Credentials = new System.Net.NetworkCredential("user", "pass"),
                    EnableSsl = true,
                };
                
                var mailMessage = new MailMessage
                {
                    From = new MailAddress("noreply@example.com"),
                    Subject = "Order Confirmation",
                    Body = $"Your order {order.Id} has been placed for {totalAmount:C}.",
                    IsBodyHtml = true,
                };
                mailMessage.To.Add(user.Email);

                // SMTP call inside web request!
                smtpClient.Send(mailMessage); 
            }
            catch (SmtpException)
            {
                // SMELL: Empty catch block 3
            }
            catch (Exception)
            {
                // SMELL: Empty catch block 4
            }

            // Return untyped anonymous object
            return new
            {
                success = true,
                orderId = order.Id,
                total = totalAmount,
                status = order.Status
            };
        }
    }

    public class CreateOrderRequest
    {
        public string Email { get; set; }
        public string ShippingAddress { get; set; }
        public List<OrderItemRequest> Items { get; set; }
    }

    public class OrderItemRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
