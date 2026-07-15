using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TechStore.Domain.DTOs;
using TechStore.Domain.DTOs.Request;
using TechStore.Domain.DTOs.Response;
using TechStore.Service.IService;

namespace TechStoreAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly IMapper _mapper;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(
            IOrderService orderService,
            IMapper mapper,
            ILogger<OrdersController> logger)
        {
            _orderService = orderService;
            _mapper = mapper;
            _logger = logger;
        }

        // ─────────────────────────────────────────────────────────────────────
        // POST /api/orders/checkout
        // Checkout from user's cart (supports COD/BankTransfer/CreditCard/Wallet)
        // ─────────────────────────────────────────────────────────────────────
        /// <summary>
        /// Places an order from the user's cart.
        /// Wallet payments debit the wallet and confirm the order immediately.
        /// VNPay is used only by /api/wallet/top-up.
        /// </summary>
        [Authorize]
        [HttpPost("checkout")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var authenticatedUserId))
                return Unauthorized(new { message = "Không xác thực được người dùng." });

            // Never trust a client-supplied user ID for wallet operations.
            request.UserId = authenticatedUserId;

            try
            {
                var result = await _orderService.CheckoutAsync(request);

                _logger.LogInformation(
                    "Order {OrderId} created — Method: {Method}",
                    result.Order.Id, request.PaymentMethod);

                return CreatedAtAction(
                    nameof(GetOrder),
                    new { id = result.Order.Id },
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
        // GET /api/orders
        // Get all orders (admin)
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<ActionResult<IEnumerable<OrderResponseDTO>>> GetOrders()
        {
            var orders = await _orderService.GetAllOrdersAsync();
            var response = _mapper.Map<IEnumerable<OrderResponseDTO>>(orders);
            return Ok(response);
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /api/orders/{id}
        // Get single order by ID
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("{id}")]
        public async Task<ActionResult<OrderResponseDTO>> GetOrder(Guid id)
        {
            var order = await _orderService.GetOrderByIdAsync(id);
            if (order == null) return NotFound("Đơn hàng không tồn tại");

            var response = _mapper.Map<OrderResponseDTO>(order);
            return Ok(response);
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /api/orders/user/{userId}
        // Get all orders for a user
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<OrderResponseDTO>>> GetOrdersByUser(Guid userId)
        {
            var orders = await _orderService.GetOrdersByUserIdAsync(userId);
            var response = _mapper.Map<IEnumerable<OrderResponseDTO>>(orders);
            return Ok(response);
        }

        // ─────────────────────────────────────────────────────────────────────
        // POST /api/orders
        // Create order manually (admin / direct create without cart)
        // ─────────────────────────────────────────────────────────────────────
        [HttpPost]
        public async Task<ActionResult<OrderResponseDTO>> CreateOrder(CreateOrderDTO createOrderDto)
        {
            if (createOrderDto.OrderDetails == null || createOrderDto.OrderDetails.Count == 0)
            {
                return BadRequest("Đơn hàng phải có ít nhất 1 sản phẩm.");
            }

            var createdOrder = await _orderService.CreateOrderAsync(createOrderDto);

            // Lấy lại order với details đầy đủ để map cho đẹp
            var orderWithDetails = await _orderService.GetOrderByIdAsync(createdOrder.Id);
            var response = _mapper.Map<OrderResponseDTO>(orderWithDetails);

            return Ok(response);
        }

        // ─────────────────────────────────────────────────────────────────────
        // PUT /api/orders/{id}/status (Accepts raw string body like "Confirmed")
        // ─────────────────────────────────────────────────────────────────────
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateOrderStatusPut(Guid id, [FromBody] string newStatus)
        {
            var existingOrder = await _orderService.GetOrderByIdAsync(id);
            if (existingOrder == null) return NotFound("Đơn hàng không tồn tại");

            await _orderService.UpdateOrderStatusAsync(id, newStatus);
            return NoContent();
        }

        // ─────────────────────────────────────────────────────────────────────
        // PATCH /api/orders/{id}/status (Accepts JSON body {"status": "Confirmed"})
        // ─────────────────────────────────────────────────────────────────────
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateOrderStatusPatch(Guid id, [FromBody] UpdateOrderStatusRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existingOrder = await _orderService.GetOrderByIdAsync(id);
            if (existingOrder == null) return NotFound("Đơn hàng không tồn tại");

            await _orderService.UpdateOrderStatusAsync(id, request.Status);
            return NoContent();
        }

        // ─────────────────────────────────────────────────────────────────────
        // DELETE /api/orders/{id}
        // Delete order (admin)
        // ─────────────────────────────────────────────────────────────────────
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(Guid id)
        {
            var existingOrder = await _orderService.GetOrderByIdAsync(id);
            if (existingOrder == null) return NotFound("Đơn hàng không tồn tại");

            await _orderService.DeleteOrderAsync(id);
            return NoContent();
        }

        // ─────────────────────────────────────────────────────────────────────
        // COMPATIBILITY ENDPOINTS (Hidden from Swagger UI, for Mobile FE)
        // ─────────────────────────────────────────────────────────────────────
        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpPost("/api/order/checkout")]
        public Task<IActionResult> CheckoutCompat([FromBody] CheckoutRequest request)
            => Checkout(request);

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet("/api/order/{id}")]
        public Task<ActionResult<OrderResponseDTO>> GetOrderCompat(Guid id)
            => GetOrder(id);

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet("/api/order/user/{userId}")]
        public Task<ActionResult<IEnumerable<OrderResponseDTO>>> GetOrdersByUserCompat(Guid userId)
            => GetOrdersByUser(userId);

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpPatch("/api/order/{id}/status")]
        public Task<IActionResult> UpdateOrderStatusPatchCompat(Guid id, [FromBody] UpdateOrderStatusRequest request)
            => UpdateOrderStatusPatch(id, request);

        // POST /api/orders/{id}/confirm-delivery
        [HttpPost("{id}/confirm-delivery")]
        public async Task<IActionResult> ConfirmDelivery(Guid id, [FromForm] TechStore.Domain.DTOs.Request.UploadDeliveryProofRequest request)
        {
            if (request == null || request.Image == null)
            {
                return BadRequest(TechStore.Domain.Constants.ShippingConstants.ImageFileRequired);
            }

            try
            {
                var result = await _orderService.ConfirmDeliveryAsync(id, request.Image);
                if (!result)
                {
                    return NotFound(TechStore.Domain.Constants.ShippingConstants.OrderNotFound);
                }

                return Ok(new { message = "Đã xác nhận giao hàng thành công." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, string.Format(TechStore.Domain.Constants.ShippingConstants.UploadProofFailed, ex.Message));
            }
        }

        // PUT /api/orders/{id}/assign-shipper?staffId=xxx
        [HttpPut("{id}/assign-shipper")]
        public async Task<IActionResult> AssignShipper(Guid id, [FromQuery] Guid staffId)
        {
            var order = await _orderService.GetOrderByIdAsync(id);
            if (order == null) return NotFound(TechStore.Domain.Constants.ShippingConstants.OrderNotFound);

            await _orderService.AssignShipperAsync(id, staffId);
            return NoContent();
        }
    }

    public class UpdateOrderStatusRequest
    {
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.RegularExpression(
            "^(Pending|PendingPayment|Confirmed|Shipped|Delivered|Cancelled)$",
            ErrorMessage = "Status must be: Pending, PendingPayment, Confirmed, Shipped, Delivered, or Cancelled.")]
        public string Status { get; set; } = string.Empty;
    }
}
