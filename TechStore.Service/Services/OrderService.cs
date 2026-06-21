using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TechStore.Domain.DTOs;
using TechStore.Domain.Models;
using TechStore.Repository.Data;
using TechStore.Service.IServices;

namespace TechStore.Service.Services
{
    public class OrderService : IOrderService
    {
        private readonly ApplicationDbContext _context;
        private readonly IVnpayService _vnpayService;

        public OrderService(ApplicationDbContext context, IVnpayService vnpayService)
        {
            _context = context;
            _vnpayService = vnpayService;
        }

        // ─────────────────────────────────────────────────────────────────────
        // CHECKOUT
        // ─────────────────────────────────────────────────────────────────────
        public async Task<CheckoutResult> CheckoutAsync(CheckoutRequest request, string ipAddress)
        {
            // 1. Load cart items for the user (include Product info)
            var cartItems = await _context.Carts
                .Where(c => c.UserId == request.UserId)
                .Include(c => c.Product)
                .ToListAsync();

            if (cartItems == null || cartItems.Count == 0)
                throw new InvalidOperationException("Cannot checkout: cart is empty.");

            // 2. Validate stock availability
            var stockErrors = new List<string>();
            foreach (var item in cartItems)
            {
                if (item.Product == null)
                {
                    stockErrors.Add($"Product with id {item.ProductId} no longer exists.");
                    continue;
                }
                if (item.Product.StockQuantity < item.Quantity)
                {
                    stockErrors.Add(
                        $"'{item.Product.Name}' only has {item.Product.StockQuantity} unit(s) in stock " +
                        $"(requested: {item.Quantity}).");
                }
            }

            if (stockErrors.Count > 0)
                throw new InvalidOperationException(
                    "Stock validation failed:\n" + string.Join("\n", stockErrors));

            // 3. Calculate total
            decimal total = cartItems.Sum(c => c.Product!.Price * c.Quantity);

            // 4. Determine initial statuses
            bool isVnpay = request.PaymentMethod.Equals("VNPay", StringComparison.OrdinalIgnoreCase);
            string orderStatus   = isVnpay ? "PendingPayment" : "Pending";
            string paymentStatus = "Pending";

            // 5. Create Order entity
            var order = new Order
            {
                Id              = Guid.NewGuid(),
                UserId          = request.UserId,
                OrderDate       = DateTime.UtcNow,
                TotalAmount     = total,
                ShippingAddress = request.ShippingAddress,
                PaymentMethod   = request.PaymentMethod,
                Status          = orderStatus,
                PaymentStatus   = paymentStatus
            };

            // 6. Create OrderDetail entities
            var orderDetails = cartItems.Select(c => new OrderDetail
            {
                Id        = Guid.NewGuid(),
                OrderId   = order.Id,
                ProductId = c.ProductId,
                Quantity  = c.Quantity,
                UnitPrice = c.Product!.Price
            }).ToList();

            // 7. Deduct stock
            foreach (var item in cartItems)
                item.Product!.StockQuantity -= item.Quantity;

            // 8. Persist everything atomically (clear cart immediately)
            await _context.Orders.AddAsync(order);
            await _context.OrderDetails.AddRangeAsync(orderDetails);
            _context.Carts.RemoveRange(cartItems);
            await _context.SaveChangesAsync();

            // 9. Load user for response mapping
            var user = await _context.Users.FindAsync(request.UserId);

            var orderResponse = MapToOrderResponse(order, orderDetails, cartItems, user);

            // 10. For VNPay → build payment URL
            string? paymentUrl = null;
            if (isVnpay)
                paymentUrl = _vnpayService.CreatePaymentUrl(order, ipAddress);

            return new CheckoutResult
            {
                Order      = orderResponse,
                PaymentUrl = paymentUrl
            };
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET ORDERS BY USER
        // ─────────────────────────────────────────────────────────────────────
        public async Task<List<OrderResponse>> GetOrdersByUserAsync(Guid userId)
        {
            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.OrderDetails).ThenInclude(od => od.Product)
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return orders.Select(MapToOrderResponse).ToList();
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET ORDER BY ID
        // ─────────────────────────────────────────────────────────────────────
        public async Task<OrderResponse?> GetOrderByIdAsync(Guid orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails).ThenInclude(od => od.Product)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            return order == null ? null : MapToOrderResponse(order);
        }

        // ─────────────────────────────────────────────────────────────────────
        // UPDATE ORDER STATUS
        // ─────────────────────────────────────────────────────────────────────
        public async Task<OrderResponse?> UpdateOrderStatusAsync(Guid orderId, string newStatus)
        {
            var validStatuses = new[] { "Pending", "PendingPayment", "Confirmed", "Shipped", "Delivered", "Cancelled" };
            if (!validStatuses.Contains(newStatus))
                throw new ArgumentException(
                    $"Invalid status '{newStatus}'. Valid values: {string.Join(", ", validStatuses)}");

            var order = await _context.Orders
                .Include(o => o.OrderDetails).ThenInclude(od => od.Product)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) return null;

            order.Status = newStatus;
            await _context.SaveChangesAsync();
            return MapToOrderResponse(order);
        }

        // ─────────────────────────────────────────────────────────────────────
        // CONFIRM VNPAY PAYMENT  (called by PaymentService / IPN handler)
        // ─────────────────────────────────────────────────────────────────────
        public async Task<bool> ConfirmVnpayPaymentAsync(Guid orderId, bool success, string transactionId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return false;

            // Idempotency guard — don't process twice
            if (order.PaymentStatus != "Pending") return true;

            if (success)
            {
                order.Status              = "Confirmed";
                order.PaymentStatus       = "Paid";
                order.VnpayTransactionId  = transactionId;
            }
            else
            {
                order.Status        = "Cancelled";
                order.PaymentStatus = "Failed";
            }

            await _context.SaveChangesAsync();
            return true;
        }

        // ─────────────────────────────────────────────────────────────────────
        // MAPPING HELPERS
        // ─────────────────────────────────────────────────────────────────────
        private static OrderResponse MapToOrderResponse(Order order)
            => new()
            {
                OrderId             = order.Id,
                UserId              = order.UserId,
                CustomerName        = order.User?.FullName ?? string.Empty,
                CustomerEmail       = order.User?.Email ?? string.Empty,
                OrderDate           = order.OrderDate,
                TotalAmount         = order.TotalAmount,
                Status              = order.Status,
                PaymentStatus       = order.PaymentStatus,
                VnpayTransactionId  = order.VnpayTransactionId,
                ShippingAddress     = order.ShippingAddress,
                PaymentMethod       = order.PaymentMethod,
                Items = order.OrderDetails?.Select(od => new OrderItemResponse
                {
                    ProductId   = od.ProductId,
                    ProductName = od.Product?.Name  ?? string.Empty,
                    Brand       = od.Product?.Brand ?? string.Empty,
                    ImageUrl    = od.Product?.ImageUrl,
                    Quantity    = od.Quantity,
                    UnitPrice   = od.UnitPrice
                }).ToList() ?? new List<OrderItemResponse>()
            };

        private static OrderResponse MapToOrderResponse(
            Order order,
            List<OrderDetail> details,
            List<Cart> cartItems,
            User? user)
        {
            var productLookup = cartItems.ToDictionary(c => c.ProductId, c => c.Product!);
            return new OrderResponse
            {
                OrderId            = order.Id,
                UserId             = order.UserId,
                CustomerName       = user?.FullName ?? string.Empty,
                CustomerEmail      = user?.Email    ?? string.Empty,
                OrderDate          = order.OrderDate,
                TotalAmount        = order.TotalAmount,
                Status             = order.Status,
                PaymentStatus      = order.PaymentStatus,
                VnpayTransactionId = order.VnpayTransactionId,
                ShippingAddress    = order.ShippingAddress,
                PaymentMethod      = order.PaymentMethod,
                Items = details.Select(d =>
                {
                    productLookup.TryGetValue(d.ProductId, out var product);
                    return new OrderItemResponse
                    {
                        ProductId   = d.ProductId,
                        ProductName = product?.Name  ?? string.Empty,
                        Brand       = product?.Brand ?? string.Empty,
                        ImageUrl    = product?.ImageUrl,
                        Quantity    = d.Quantity,
                        UnitPrice   = d.UnitPrice
                    };
                }).ToList()
            };
        }
    }
}
