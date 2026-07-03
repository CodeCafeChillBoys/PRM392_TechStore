using System;
using TechStore.Domain.DTOs;

namespace TechStore.Service.IService
{
    public interface ITrackingService
    {
        void UpdateLocation(Guid shipperId, double lat, double lng);
        TrackingLocation? GetLatestLocation(Guid shipperId);

        // Lưu/đọc vị trí theo ĐƠN HÀNG. Khách theo dõi 1 đơn, nên cần thấy đúng
        // toạ độ của shipper đang chạy đơn đó — kể cả khi nhân viên bấm "Xem & Chạy"
        // khác nhân viên được gán ban đầu (tránh đọc nhầm vị trí cũ theo StaffId).
        void UpdateLocationForOrder(Guid orderId, Guid shipperId, double lat, double lng);
        TrackingLocation? GetLatestLocationByOrder(Guid orderId);
    }
}