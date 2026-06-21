using Microsoft.AspNetCore.Mvc;
using TechStore.Domain.DTOs;
using TechStore.Service.IServices;

namespace TechStoreAPI.Controllers
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

        // ─────────────────────────────────────────────────────────────────────
        // POST /api/order/checkout
        // ─────────────────────────────────────────────────────────────────────
        /// <summary>
        /// Places an order from the user's cart.
        /// - COD/BankTransfer/CreditCard: returns full order details (status=Pending).
        /// - VNPay: returns order + paymentUrl (status=PendingPayment). Client must open paymentUrl.
        /// </summary>
        [HttpPost("checkout")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                // Get client IP for VNPay payment URL (required by VNPay spec)
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

                var result = await _orderService.CheckoutAsync(request, ipAddress);

                _logger.LogInformation(
                    "Order {OrderId} created — Method: {Method}, RequiresPayment: {RequiresPayment}",
                    result.Order.OrderId, request.PaymentMethod, result.RequiresOnlinePayment);

                // VNPay flow → return paymentUrl for client to open
                if (result.RequiresOnlinePayment)
                {
                    return CreatedAtAction(
                        nameof(GetOrderById),
                        new { orderId = result.Order.OrderId },
                        new
                        {
                            message            = "Order created. Please complete payment via VNPay.",
                            requiresPayment    = true,
                            paymentUrl         = result.PaymentUrl,
                            orderId            = result.Order.OrderId,
                            totalAmount        = result.Order.TotalAmount,
                            orderStatus        = result.Order.Status,
                            paymentStatus      = result.Order.PaymentStatus
                        });
                }

                // COD / BankTransfer / CreditCard → order confirmed immediately
                return CreatedAtAction(
                    nameof(GetOrderById),
                    new { orderId = result.Order.OrderId },
                    new
                    {
                        message         = "Order placed successfully.",
                        requiresPayment = false,
                        data            = result.Order
                    });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Checkout failed for user {UserId}: {Message}", request.UserId, ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during checkout for user {UserId}.", request.UserId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An unexpected error occurred. Please try again." });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /api/order/user/{userId}
        // ─────────────────────────────────────────────────────────────────────
        /// <summary>Returns all orders for a user, newest first.</summary>
        [HttpGet("user/{userId:guid}")]
        [ProducesResponseType(typeof(List<OrderResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetOrdersByUser(Guid userId)
        {
            var orders = await _orderService.GetOrdersByUserAsync(userId);

            if (orders == null || orders.Count == 0)
                return NotFound(new { message = $"No orders found for user {userId}." });

            return Ok(new
            {
                message = "Orders retrieved successfully.",
                total   = orders.Count,
                data    = orders
            });
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /api/order/{orderId}
        // ─────────────────────────────────────────────────────────────────────
        /// <summary>Returns a single order by ID.</summary>
        [HttpGet("{orderId:guid}")]
        [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetOrderById(Guid orderId)
        {
            var order = await _orderService.GetOrderByIdAsync(orderId);

            if (order == null)
                return NotFound(new { message = $"Order {orderId} not found." });

            return Ok(new { message = "Order retrieved successfully.", data = order });
        }

        // ─────────────────────────────────────────────────────────────────────
        // PATCH /api/order/{orderId}/status
        // ─────────────────────────────────────────────────────────────────────
        /// <summary>
        /// Updates order status manually (admin use).
        /// Valid: Pending | PendingPayment | Confirmed | Shipped | Delivered | Cancelled
        /// </summary>
        [HttpPatch("{orderId:guid}/status")]
        [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateOrderStatus(
            Guid orderId,
            [FromBody] UpdateOrderStatusRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var order = await _orderService.UpdateOrderStatusAsync(orderId, request.Status);

                if (order == null)
                    return NotFound(new { message = $"Order {orderId} not found." });

                return Ok(new
                {
                    message = $"Order status updated to '{request.Status}'.",
                    data    = order
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }

    // ─── Inline request model ───────────────────────────────────────────────
    public class UpdateOrderStatusRequest
    {
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.RegularExpression(
            "^(Pending|PendingPayment|Confirmed|Shipped|Delivered|Cancelled)$",
            ErrorMessage = "Status must be: Pending, PendingPayment, Confirmed, Shipped, Delivered, or Cancelled.")]
        public string Status { get; set; } = string.Empty;
    }
}
