using AutoMapper;
using System.Data;
using Microsoft.EntityFrameworkCore;
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
    public partial class OrderService : IService.IOrderService
    {
        private readonly ApplicationDbContext _context;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly INotificationService _notificationService;

        public OrderService(
            ApplicationDbContext context,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            INotificationService notificationService)
        {
            _context = context;
            _unitOfWork = unitOfWork;
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
                Latitude = orderDto.Latitude,
                Longitude = orderDto.Longitude,
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
            await using var dbTransaction = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);

            var order = await _context.Orders
                .FromSqlInterpolated($"SELECT * FROM \"Orders\" WHERE \"Id\" = {id} FOR UPDATE")
                .SingleOrDefaultAsync();

            if (order == null)
                return;

            var isCancellation = newStatus.Equals("Cancelled", StringComparison.OrdinalIgnoreCase);
            if (isCancellation && order.Status.Equals("Delivered", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Không thể hủy đơn hàng đã giao thành công.");

            if (isCancellation)
            {
                await RefundPaidWalletOrderAsync(order);
                order.Status = "Cancelled";
            }
            else
            {
                order.Status = newStatus;
                // COD/khác Ví: giao xong = shipper đã thu tiền mặt → ghi nhận đã thanh toán.
                if (newStatus.Equals("Delivered", StringComparison.OrdinalIgnoreCase))
                {
                    if (order.PaymentStatus.Equals("Pending", StringComparison.OrdinalIgnoreCase))
                    {
                        order.PaymentStatus = "Paid";
                    }
                    // Ghi mốc giao hàng để tính cửa sổ hoàn tiền; chỉ set lần đầu để re-run không dời mốc.
                    order.DeliveredAt ??= DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();
        }

        private async Task RefundPaidWalletOrderAsync(Order order)
        {
            if (!order.PaymentMethod.Equals("Wallet", StringComparison.OrdinalIgnoreCase) ||
                !order.PaymentStatus.Equals("Paid", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // The unique (OrderId, Type) index and the locked order make this idempotent.
            var existingRefund = await _context.WalletTransactions
                .AnyAsync(transaction => transaction.OrderId == order.Id &&
                                         transaction.Type == WalletTransactionType.Refund);
            if (existingRefund)
            {
                order.PaymentStatus = "Refunded";
                return;
            }

            var paymentTransaction = await _context.WalletTransactions
                .SingleOrDefaultAsync(transaction => transaction.OrderId == order.Id &&
                                                     transaction.Type == WalletTransactionType.Payment &&
                                                     transaction.Status == WalletTransactionStatus.Completed);

            if (paymentTransaction == null)
                throw new InvalidOperationException("Không tìm thấy giao dịch thanh toán ví của đơn hàng.");

            var wallet = await _context.Wallets
                .FromSqlInterpolated($"SELECT * FROM \"Wallets\" WHERE \"Id\" = {paymentTransaction.WalletId} FOR UPDATE")
                .SingleAsync();

            var before = wallet.Balance;
            wallet.Balance += paymentTransaction.Amount;
            wallet.UpdatedAt = DateTime.UtcNow;

            await _context.WalletTransactions.AddAsync(new WalletTransaction
            {
                Id = Guid.NewGuid(),
                WalletId = wallet.Id,
                Type = WalletTransactionType.Refund,
                Status = WalletTransactionStatus.Completed,
                Amount = paymentTransaction.Amount,
                BalanceBefore = before,
                BalanceAfter = wallet.Balance,
                Description = $"Hoàn tiền đơn hàng {order.Id}",
                OrderId = order.Id,
                CreatedAt = DateTime.UtcNow,
                ProcessedAt = DateTime.UtcNow
            });

            order.PaymentStatus = "Refunded";
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
        // CHECKOUT (wallet payments and order data are committed atomically)
        // =====================================================================

        public async Task<CheckoutResult> CheckoutAsync(CheckoutRequest request)
        {
            await using var dbTransaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

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

            // Chặn thao túng giá: phí ship do FE gửi nên không tin tuyệt đối.
            // Chặn số âm (trả ít hơn) và số vô lý (ship thực tế theo km chỉ vài trăm nghìn).
            if (request.ShippingFee < 0 || request.ShippingFee > 10_000_000)
                throw new InvalidOperationException("Phí ship không hợp lệ.");

            // Tổng khách trả = tiền hàng + phí ship (khớp số FE hiển thị + số trừ ví).
            total += request.ShippingFee;

            // 4. Wallet payments are completed immediately; other methods remain pending.
            bool isWallet = request.PaymentMethod.Equals("Wallet", StringComparison.OrdinalIgnoreCase);
            string orderStatus = isWallet ? "Confirmed" : "Pending";
            string paymentStatus = isWallet ? "Paid" : "Pending";

            // 5. Create Order entity
            var order = new Order
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                OrderDate = DateTime.UtcNow,
                TotalAmount = total,
                ShippingFee = request.ShippingFee,
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

            // 7. Lock and debit the wallet before creating a paid order.
            WalletTransaction? walletTransaction = null;
            if (isWallet)
            {
                var wallet = await _context.Wallets
                    .FromSqlInterpolated($"SELECT * FROM \"Wallets\" WHERE \"UserId\" = {request.UserId} FOR UPDATE")
                    .SingleOrDefaultAsync();

                if (wallet == null)
                    throw new InvalidOperationException("Không tìm thấy ví của người dùng.");

                if (wallet.Balance < total)
                    throw new InvalidOperationException(
                        $"Số dư ví không đủ. Số dư hiện tại: {wallet.Balance:N0}đ, cần thanh toán: {total:N0}đ.");

                var before = wallet.Balance;
                wallet.Balance -= total;
                wallet.UpdatedAt = DateTime.UtcNow;

                walletTransaction = new WalletTransaction
                {
                    Id = Guid.NewGuid(),
                    WalletId = wallet.Id,
                    Type = WalletTransactionType.Payment,
                    Status = WalletTransactionStatus.Completed,
                    Amount = total,
                    BalanceBefore = before,
                    BalanceAfter = wallet.Balance,
                    Description = $"Thanh toán đơn hàng {order.Id}",
                    OrderId = order.Id,
                    CreatedAt = DateTime.UtcNow,
                    ProcessedAt = DateTime.UtcNow
                };
            }

            // 8. Deduct stock
            foreach (var item in cartItems)
                item.Product!.StockQuantity -= item.Quantity;

            // 9. Persist order, stock, cart and wallet ledger atomically.
            await _context.Orders.AddAsync(order);
            await _context.OrderDetails.AddRangeAsync(orderDetails);
            if (walletTransaction != null)
                await _context.WalletTransactions.AddAsync(walletTransaction);
            _context.Carts.RemoveRange(cartItems);
            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            // 10. Populate relations for AutoMapper.
            var user = await _context.Users.FindAsync(request.UserId);
            var productLookup = cartItems.ToDictionary(c => c.ProductId, c => c.Product!);

            order.User = user!;
            foreach (var detail in orderDetails)
            {
                if (productLookup.TryGetValue(detail.ProductId, out var product))
                {
                    detail.Product = product;
                }
            }
            order.OrderDetails = orderDetails;

            var orderResponse = _mapper.Map<OrderResponseDTO>(order);

            await _notificationService.CreateAndSendNotificationAsync(
               new CreateNotificationRequest
               {
                   UserId = request.UserId,
                   Title = isWallet
                       ? $"💳 Thanh toán thành công đơn #{order.Id.ToString()[..8]}"
                       : $"🎉 Đặt hàng thành công đơn #{order.Id.ToString()[..8]}",
                   Body = isWallet
                       ? "Đơn hàng đã được thanh toán bằng số dư ví. TechStore đang chuẩn bị hàng để giao cho bạn."
                       : "Đơn hàng của bạn đã được tiếp nhận và đang chờ duyệt.",
                   Type = NotificationType.Order,
                   Icon = NotificationIcon.Gift,
                   Tone = NotificationTone.Accent
               }
            );

            return new CheckoutResult
            {
                Order = orderResponse
            };
        }


    }
}
