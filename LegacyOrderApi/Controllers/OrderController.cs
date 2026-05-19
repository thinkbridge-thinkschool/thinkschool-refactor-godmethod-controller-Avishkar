using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using LegacyOrderApi.DTOs;
using LegacyOrderApi.Services;
using Microsoft.Extensions.Logging;

namespace LegacyOrderApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly ILogger<OrderController> _logger;

        public OrderController(IOrderService orderService, ILogger<OrderController> logger)
        {
            _orderService = orderService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult<OrderResponse>> Post([FromBody] CreateOrderRequest request, CancellationToken cancellationToken)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var response = await _orderService.CreateOrderAsync(request, cancellationToken);
                
                if (!response.Success)
                {
                    return BadRequest(new ErrorResponse { Message = response.Status });
                }

                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument provided for order creation.");
                return BadRequest(new ErrorResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                // Fix: No more swallowed exceptions. Log and return 500.
                _logger.LogError(ex, "An unexpected error occurred while processing the order.");
                return StatusCode(500, new ErrorResponse { Message = "An unexpected error occurred." });
            }
        }
    }
}
