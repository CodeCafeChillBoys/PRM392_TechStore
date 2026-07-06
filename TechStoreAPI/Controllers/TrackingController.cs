using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TechStore.Domain.Constants;
using TechStore.Domain.DTOs.Request;
using TechStore.Service.IService;
using TechStoreAPI.Hubs;

namespace TechStoreAPI.Controllers
{
    [ApiController]
    [Route("api/tracking")]
    public class TrackingController : ControllerBase
    {
        private readonly ITrackingService _trackingService;
        private readonly IOrderService _orderService;
        private readonly IHubContext<TrackingHub> _hubContext;

        public TrackingController(
            ITrackingService trackingService,

            IOrderService orderService,
            IHubContext<TrackingHub> hubContext)
        {
            _trackingService = trackingService;
            _orderService = orderService;
            _hubContext = hubContext;
        }

        [HttpPost("location")]
        public async Task<IActionResult> UpdateLocation([FromBody] UpdateShipperLocationRequest request)
        {
            if (request == null || request.ShipperId == Guid.Empty)
            {
                return BadRequest(ShippingConstants.TrackingDataInvalid);
            }

            // Chặn toạ độ rác (GPS mặc định của máy ảo: (0,0) hoặc Mountain View)
            // để không hiển thị xe sai vị trí cho khách.
            if (!IsWithinVietnam(request.Lat, request.Lng))
            {
                return BadRequest(ShippingConstants.LocationOutsideServiceArea);
            }

            // A. Lưu toạ độ mới nhất của Shipper vào RAM
            _trackingService.UpdateLocation(request.ShipperId, request.Lat, request.Lng);

            // B. Nếu client đã gửi đúng OrderId thì phát trực tiếp vào phòng của đơn đó
            if (request.OrderId.HasValue && request.OrderId.Value != Guid.Empty)
            {
                // Lưu thêm theo ĐƠN để GET/khách đọc đúng shipper đang chạy đơn này
                // (không phụ thuộc StaffId được gán ban đầu).
                _trackingService.UpdateLocationForOrder(
                    request.OrderId.Value, request.ShipperId, request.Lat, request.Lng);

                // CHỈ PHÁT REALTIME tới khách hàng của đơn hàng cụ thể này
                await _hubContext.Clients.Group(request.OrderId.Value.ToString()).SendAsync("ReceiveLocation", new
                {
                    lat = request.Lat,
                    lng = request.Lng,
                    updatedAt = DateTime.UtcNow
                });
            }
            else
            {
                // C. Fallback: phát tới tất cả đơn đang active của shipper
                var activeOrderIds = await _orderService.GetActiveOrderIdsByShipperAsync(request.ShipperId);
                foreach (var orderId in activeOrderIds)
                {
                    await _hubContext.Clients.Group(orderId.ToString()).SendAsync("ReceiveLocation", new
                    {
                        lat = request.Lat,
                        lng = request.Lng,
                        updatedAt = DateTime.UtcNow
                    });
                }
            }

            return Ok(new { message = ShippingConstants.UpdateLocationSuccess });
        }

        [HttpGet("order/{orderId}")]
        public async Task<IActionResult> GetLocationByOrder(Guid orderId)
        {
            // A. Lấy thông tin đơn hàng thông qua OrderService
            var order = await _orderService.GetOrderByIdAsync(orderId);
            if (order == null)
            {
                return NotFound(ShippingConstants.OrderNotFound);
            }

            // Kiểm tra tính hợp lệ
            if (order.StaffId == null || order.Status != ShippingConstants.StatusDelivering)
            {
                return BadRequest(ShippingConstants.OrderNotDelivering);
            }

            // B. Chỉ đọc toạ độ lưu THEO ĐƠN (shipper đang thực sự "Xem & Chạy" đơn này).
            //    KHÔNG fallback về vị trí theo StaffId nữa — fallback khiến các đơn khác
            //    của cùng shipper trả về chung 1 toạ độ (vị trí shipper), gây hiểu nhầm
            //    khách đang xem đơn nào cũng thấy xe. Đơn chưa chạy -> coi như chưa có vị trí.
            var location = _trackingService.GetLatestLocationByOrder(orderId);
            // Toạ độ cũ ngoài VN (GPS mặc định máy ảo còn sót trong RAM) coi như chưa có.
            if (location == null || !IsWithinVietnam(location.Lat, location.Lng))
            {
                return NotFound(ShippingConstants.ShipperLocationNotFound);
            }

            return Ok(new
            {
                lat = location.Lat,
                lng = location.Lng,
                updatedAt = location.UpdatedAt
            });
        }

        // Toạ độ có nằm trong phạm vi Việt Nam không (chặn GPS mặc định của máy ảo).
        private static bool IsWithinVietnam(double lat, double lng)
        {
            return lat >= ShippingConstants.VietnamMinLatitude
                && lat <= ShippingConstants.VietnamMaxLatitude
                && lng >= ShippingConstants.VietnamMinLongitude
                && lng <= ShippingConstants.VietnamMaxLongitude;
        }
    }
}