using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using TechStore.Domain.Constants;
using TechStore.Domain.DTOs;
using TechStore.Domain.DTOs.Request;
using TechStore.Domain.DTOs.Response;
using TechStore.Domain.Enum;
using TechStore.Domain.Models;
using TechStore.Repository.Data;
using TechStore.Repository.IRepositories;
using TechStore.Service.IService;

namespace TechStore.Service.Service
{
    public class OrderService : IService.IOrderService
    {
        private readonly ApplicationDbContext _context;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IVnpayService _vnpayService;
        private readonly IMapper _mapper;
        private readonly INotificationService _notificationService;

        public OrderService(
            ApplicationDbContext context,
            IUnitOfWork unitOfWork,
            IVnpayService vnpayService,
            IMapper mapper,
            INotificationService notificationService)
        {
            _context = context;
            _unitOfWork = unitOfWork;
            _vnpayService = vnpayService;
            _mapper = mapper;
            _notificationService = notificationService;
        }

        // =====================================================================
        // CRUD (from develop branch — uses UnitOfWork/Repository pattern)
        // =====================================================================

        public async Task<IEnumerable<Order>> GetAllOrdersAsync()
        {
            return await _unitOfWork.Orders.GetOrdersWithDetailsAsync();
        }

        public async Task<Order?> GetOrderByIdAsync(Guid id)
        {
            return await _unitOfWork.Orders.GetOrderByIdWithDetailsAsync(id);
        }

        public async Task<IEnumerable<Order>> GetOrdersByUserIdAsync(Guid userId)
        {
            return await _unitOfWork.Orders.GetOrdersByUserIdAsync(userId);
        }

        public async Task<Order> CreateOrderAsync(CreateOrderDTO orderDto)
        {
            var order = new Order
            {
                UserId = orderDto.UserId,
                ShippingAddress = orderDto.ShippingAddress,
                PaymentMethod = orderDto.PaymentMethod,
                OrderDate = DateTime.UtcNow,
                Status = "Pending",
                OrderDetails = new List<OrderDetail>()
            };

            decimal totalAmount = 0;

            foreach (var detail in orderDto.OrderDetails)
            {
                var orderDetail = new OrderDetail
                {
                    ProductId = detail.ProductId,
                    Quantity = detail.Quantity,
                    UnitPrice = detail.UnitPrice
                };
                totalAmount += detail.Quantity * detail.UnitPrice;
                order.OrderDetails.Add(orderDetail);
            }

            order.TotalAmount = totalAmount;

            await _unitOfWork.Orders.AddAsync(order);
            await _unitOfWork.CompleteAsync();

            return order;
        }

        public async Task UpdateOrderStatusAsync(Guid id, string newStatus)
        {
            var order = await _unitOfWork.Orders.GetByIdAsync(id);
            if (order != null)
            {
                order.Status = newStatus;
                _unitOfWork.Orders.Update(order);
                await _unitOfWork.CompleteAsync();
            }
        }

        public async Task DeleteOrderAsync(Guid id)
        {
            var order = await _unitOfWork.Orders.GetOrderByIdWithDetailsAsync(id);
            if (order != null)
            {
                _unitOfWork.Orders.Remove(order);
                await _unitOfWork.CompleteAsync();
            }
        }

        // =====================================================================
        // CHECKOUT + VNPAY (from checkout/billing branch — uses DbContext)
        // =====================================================================

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
            string orderStatus = isVnpay ? "PendingPayment" : "Pending";
            string paymentStatus = "Pending";

            // 5. Create Order entity
            var order = new Order
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                OrderDate = DateTime.UtcNow,
                TotalAmount = total,
                ShippingAddress = request.ShippingAddress,
                PaymentMethod = request.PaymentMethod,
                Status = orderStatus,
                PaymentStatus = paymentStatus
            };

            // 6. Create OrderDetail entities
            var orderDetails = cartItems.Select(c => new OrderDetail
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                ProductId = c.ProductId,
                Quantity = c.Quantity,
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

            // 9. Load user and populate relations for AutoMapper
            var user = await _context.Users.FindAsync(request.UserId);
            var productLookup = cartItems.ToDictionary(c => c.ProductId, c => c.Product!);

            order.User = user;
            foreach (var detail in orderDetails)
            {
                if (productLookup.TryGetValue(detail.ProductId, out var product))
                {
                    detail.Product = product;
                }
            }
            order.OrderDetails = orderDetails;

            var orderResponse = _mapper.Map<OrderResponseDTO>(order);

            // 10. For VNPay → build payment URL
            string? paymentUrl = null;
            if (isVnpay)
                paymentUrl = _vnpayService.CreatePaymentUrl(order, ipAddress);

            if (!isVnpay)
            {
                await _notificationService.CreateAndSendNotificationAsync(
                   new CreateNotificationRequest
                   {
                       UserId = request.UserId,
                       Title = $"🎉 Đặt hàng thành công đơn #{order.Id.ToString()[..8]}",
                       Body = "Đơn hàng của bạn đã được tiếp nhận và đang chờ duyệt.",
                       Type = NotificationType.Order,
                       Icon = NotificationIcon.Gift,
                       Tone = NotificationTone.Accent
                   }
                );
            }

            return new CheckoutResult
            {
                Order = orderResponse,
                PaymentUrl = paymentUrl
            };
        }

        public async Task<bool> ConfirmVnpayPaymentAsync(Guid orderId, bool success, string transactionId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return false;

            // Idempotency guard — don't process twice
            if (order.PaymentStatus != "Pending") return true;

            if (success)
            {
                order.Status = "Confirmed";
                order.PaymentStatus = "Paid";
                order.VnpayTransactionId = transactionId;

                await _context.SaveChangesAsync();

                await _notificationService.CreateAndSendNotificationAsync(
                   new CreateNotificationRequest
                   {
                       UserId = order.UserId,
                       Title = $"💳 Thanh toán thành công đơn #{order.Id.ToString()[..8]}",
                       Body = "Giao dịch VNPay thành công. TechStore đang chuẩn bị hàng để giao cho bạn.",
                       Type = NotificationType.Order,
                       Icon = NotificationIcon.Gift,
                       Tone = NotificationTone.Accent
                   }
                );
            }
            else
            {
                order.Status = "Cancelled";
                order.PaymentStatus = "Failed";

                await _context.SaveChangesAsync();

                await _notificationService.CreateAndSendNotificationAsync(
                   new CreateNotificationRequest
                   {
                       UserId = order.UserId,
                       Title = $"❌ Giao dịch VNPay thất bại",
                       Body = $"Thanh toán đơn hàng #{order.Id.ToString()[..8]} không thành công. Đơn hàng đã bị hủy.",
                       Type = NotificationType.Order,
                       Icon = NotificationIcon.Bell,
                       Tone = NotificationTone.Error
                   }
                );
            }

            return true;
        }

        public async Task<IEnumerable<Guid>> GetActiveOrderIdsByShipperAsync(Guid shipperId)
        {
            // Tìm các đơn hàng được gán cho nhân viên (StaffId) này và đang đi giao (Delivering)
            var orders = await _unitOfWork.Orders.FindAsync(o => o.StaffId == shipperId && o.Status == ShippingConstants.StatusDelivering);
            return orders.Select(o => o.Id);
        }

        public async Task<bool> ConfirmDeliveryAsync(Guid orderId, IFormFile imageFile)
        {
            var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
            if (order == null) return false;

            // 1. Lưu ảnh cục bộ vào thư mục wwwroot/uploads/delivery-proofs/
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "delivery-proofs");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(fileStream);
            }

            // 2. Cập nhật thông tin đơn hàng
            order.Status = ShippingConstants.StatusDelivered;
            order.DeliveryProofImageUrl = $"/uploads/delivery-proofs/{uniqueFileName}";

            _unitOfWork.Orders.Update(order);
            await _unitOfWork.CompleteAsync();

            // 3. Gửi thông báo đến Khách hàng qua NotificationService
            await _notificationService.CreateAndSendNotificationAsync(new CreateNotificationRequest
            {
                UserId = order.UserId,
                Title = "📦 Giao hàng thành công",
                Body = $"Đơn hàng #{order.Id.ToString()[..8]} đã được giao thành công.",
                Type = NotificationType.Order,
                Icon = NotificationIcon.Truck,
                Tone = NotificationTone.Success
            });

            return true;
        }

        public async Task AssignShipperAsync(Guid orderId, Guid staffId)
        {
            var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
            if (order != null)
            {
                order.StaffId = staffId;
                order.Status = ShippingConstants.StatusDelivering; // Tự động đổi sang Shipped
                
                _unitOfWork.Orders.Update(order);
                await _unitOfWork.CompleteAsync();
            }
        }
    }
}
