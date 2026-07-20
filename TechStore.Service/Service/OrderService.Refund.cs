using System.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using TechStore.Domain.Constants;
using TechStore.Domain.DTOs.Request;
using TechStore.Domain.Enum;
using TechStore.Domain.Models;

namespace TechStore.Service.Service
{
    public partial class OrderService
    {
        // =====================================================================
        // REFUND (hoàn tiền về Ví) — TH2: hoàn sau khi đã giao
        // State machine: Paid --(khách yêu cầu)--> RefundRequested
        //                    --(staff duyệt)--> Refunded (cộng ví + cộng kho)
        //                    --(staff từ chối)--> Paid
        // =====================================================================

        public async Task<bool> RequestRefundAsync(Guid orderId, Guid userId, string reason, IFormFile? image)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new InvalidOperationException("Vui lòng nhập lý do hoàn tiền.");

            var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
            if (order == null) return false;
            if (order.UserId != userId) return false;
            if (!order.Status.Equals(ShippingConstants.StatusDelivered, StringComparison.OrdinalIgnoreCase)) return false;
            if (!order.PaymentStatus.Equals("Paid", StringComparison.OrdinalIgnoreCase)) return false;

            // Cửa sổ hoàn tiền: chỉ trong 1 ngày kể từ khi nhận hàng.
            if (order.DeliveredAt == null ||
                DateTime.UtcNow - order.DeliveredAt.Value > TimeSpan.FromDays(1))
            {
                throw new InvalidOperationException(
                    "Đã quá hạn yêu cầu hoàn tiền (chỉ trong vòng 1 ngày sau khi nhận hàng).");
            }

            // Lưu ảnh minh chứng (nếu có) — validate đuôi + dung lượng + magic bytes trước khi ghi.
            if (image != null)
            {
                var safeExt = await ValidateRefundImageAsync(image);

                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "refunds");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = Guid.NewGuid().ToString() + safeExt;
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(fileStream);
                }

                order.RefundImageUrl = $"/uploads/refunds/{uniqueFileName}";
            }
            else
            {
                // Yêu cầu mới không đính ảnh → xoá ảnh của yêu cầu cũ (tránh lẫn minh chứng).
                order.RefundImageUrl = null;
            }

            order.PaymentStatus = "RefundRequested";
            order.RefundReason = reason.Trim();
            order.RefundRequestedAt = DateTime.UtcNow;

            _unitOfWork.Orders.Update(order);
            await _unitOfWork.CompleteAsync();

            // Báo cho khách biết yêu cầu đã được ghi nhận, đang chờ staff duyệt.
            await _notificationService.CreateAndSendNotificationAsync(new CreateNotificationRequest
            {
                UserId = order.UserId,
                Title = "🔄 Đã gửi yêu cầu hoàn tiền",
                Body = "Đã gửi yêu cầu hoàn tiền, đang chờ duyệt.",
                Type = NotificationType.Order,
                Icon = NotificationIcon.Bell,
                Tone = NotificationTone.Warning
            });

            return true;
        }

        public async Task<bool> ApproveRefundAsync(Guid orderId)
        {
            await using var dbTransaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            // 1. Khoá + tải đơn hàng (giống pattern UpdateOrderStatusAsync/CheckoutAsync).
            var order = await _context.Orders
                .FromSqlInterpolated($"SELECT * FROM \"Orders\" WHERE \"Id\" = {orderId} FOR UPDATE")
                .SingleOrDefaultAsync();

            if (order == null || !order.PaymentStatus.Equals("RefundRequested", StringComparison.OrdinalIgnoreCase))
                return false;

            // 2. Idempotency: nếu đã có giao dịch Refund cho đơn này (retry/double-click) → chỉ đồng bộ trạng thái.
            var existingRefund = await _context.WalletTransactions
                .AnyAsync(t => t.OrderId == order.Id && t.Type == WalletTransactionType.Refund);

            if (existingRefund)
            {
                order.PaymentStatus = "Refunded";
                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();
                return true;
            }

            // 3. Lấy hoặc tạo ví khách — đơn COD có thể chưa từng có ví.
            var wallet = await _context.Wallets
                .FromSqlInterpolated($"SELECT * FROM \"Wallets\" WHERE \"UserId\" = {order.UserId} FOR UPDATE")
                .SingleOrDefaultAsync();

            if (wallet == null)
            {
                wallet = new Wallet
                {
                    Id = Guid.NewGuid(),
                    UserId = order.UserId,
                    Balance = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _context.Wallets.AddAsync(wallet);
            }

            var before = wallet.Balance;
            wallet.Balance += order.TotalAmount;
            wallet.UpdatedAt = DateTime.UtcNow;

            await _context.WalletTransactions.AddAsync(new WalletTransaction
            {
                Id = Guid.NewGuid(),
                WalletId = wallet.Id,
                Type = WalletTransactionType.Refund,
                Status = WalletTransactionStatus.Completed,
                Amount = order.TotalAmount,
                BalanceBefore = before,
                BalanceAfter = wallet.Balance,
                Description = $"Hoàn tiền đơn hàng {order.Id.ToString()[..8]}",
                OrderId = order.Id,
                CreatedAt = DateTime.UtcNow,
                ProcessedAt = DateTime.UtcNow
            });

            // 4. Cộng lại kho cho từng sản phẩm trong đơn.
            var orderDetails = await _context.OrderDetails
                .Where(od => od.OrderId == order.Id)
                .Include(od => od.Product)
                .ToListAsync();

            foreach (var detail in orderDetails)
            {
                if (detail.Product != null)
                    detail.Product.StockQuantity += detail.Quantity;
            }

            order.PaymentStatus = "Refunded";

            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            await _notificationService.CreateAndSendNotificationAsync(new CreateNotificationRequest
            {
                UserId = order.UserId,
                Title = "💰 Đã hoàn tiền vào ví",
                Body = $"💰 Đã hoàn tiền {order.TotalAmount:N0}đ vào ví của bạn.",
                Type = NotificationType.Order,
                Icon = NotificationIcon.Gift,
                Tone = NotificationTone.Success
            });

            return true;
        }

        public async Task<bool> RejectRefundAsync(Guid orderId)
        {
            await using var dbTransaction = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);

            var order = await _context.Orders
                .FromSqlInterpolated($"SELECT * FROM \"Orders\" WHERE \"Id\" = {orderId} FOR UPDATE")
                .SingleOrDefaultAsync();

            if (order == null || !order.PaymentStatus.Equals("RefundRequested", StringComparison.OrdinalIgnoreCase))
                return false;

            order.PaymentStatus = "Paid";

            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            await _notificationService.CreateAndSendNotificationAsync(new CreateNotificationRequest
            {
                UserId = order.UserId,
                Title = "Yêu cầu hoàn tiền đã bị từ chối",
                Body = "Yêu cầu hoàn tiền đã bị từ chối.",
                Type = NotificationType.Order,
                Icon = NotificationIcon.Bell,
                Tone = NotificationTone.Warning
            });

            return true;
        }

        /// <summary>Chữ ký byte đầu file cho từng đuôi ảnh hợp lệ.</summary>
        private static readonly Dictionary<string, byte[]> _refundImageSignatures = new()
        {
            [".jpg"] = new byte[] { 0xFF, 0xD8, 0xFF },
            [".jpeg"] = new byte[] { 0xFF, 0xD8, 0xFF },
            [".png"] = new byte[] { 0x89, 0x50, 0x4E, 0x47 },
            [".webp"] = new byte[] { 0x52, 0x49, 0x46, 0x46 }, // RIFF
        };

        /// <summary>
        /// Kiểm tra ảnh minh chứng hoàn tiền: đuôi trong whitelist, ≤5MB, và byte
        /// đầu (magic bytes) khớp — chặn file giả mạo đuôi ảnh. Trả đuôi đã chuẩn
        /// hoá (không tin FileName của client), ném lỗi nếu không hợp lệ.
        /// </summary>
        private static async Task<string> ValidateRefundImageAsync(IFormFile image)
        {
            var ext = Path.GetExtension(image.FileName).ToLowerInvariant();
            if (!_refundImageSignatures.TryGetValue(ext, out var signature))
                throw new InvalidOperationException("Ảnh minh chứng phải là JPG, PNG hoặc WEBP.");
            if (image.Length > 5 * 1024 * 1024)
                throw new InvalidOperationException("Ảnh minh chứng tối đa 5MB.");

            var header = new byte[signature.Length];
            using (var stream = image.OpenReadStream())
            {
                var read = await stream.ReadAsync(header, 0, header.Length);
                if (read < signature.Length || !header.SequenceEqual(signature))
                    throw new InvalidOperationException("File không phải ảnh hợp lệ.");
            }
            return ext == ".jpeg" ? ".jpg" : ext; // chuẩn hoá jpeg → jpg
        }
    }
}
