using Microsoft.AspNetCore.Http;
using TechStore.Domain.Constants;
using TechStore.Domain.DTOs.Request;
using TechStore.Domain.Enum;
namespace TechStore.Service.Service
{
    public partial class OrderService
    {
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

            // COD/khác VNPay: giao xong = shipper đã thu tiền mặt → ghi nhận đã thanh toán.
            if (order.Status == "Delivered"
                && !string.Equals(order.PaymentMethod, "VNPay", StringComparison.OrdinalIgnoreCase)
                && order.PaymentStatus == "Pending")
            {
                order.PaymentStatus = "Paid";
            }

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