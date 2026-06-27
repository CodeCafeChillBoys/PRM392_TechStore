using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TechStore.Domain.Constants;
using TechStore.Domain.DTOs;
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

            // A. Lưu toạ độ mới nhất của Shipper vào RAM
            _trackingService.UpdateLocation(request.ShipperId, request.Lat, request.Lng);

            // B. Tìm các ID đơn hàng đang hoạt động của Shipper thông qua OrderService
            var activeOrderIds = await _orderService.GetActiveOrderIdsByShipperAsync(request.ShipperId);

            // C. Phát realtime qua SignalR Hub tới từng OrderGroup
            foreach (var orderId in activeOrderIds)
            {
                await _hubContext.Clients.Group(orderId.ToString()).SendAsync("ReceiveLocation", new
                {
                    lat = request.Lat,
                    lng = request.Lng,
                    updatedAt = DateTime.UtcNow
                });
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

            // B. Lấy toạ độ shipper đó từ RAM
            var location = _trackingService.GetLatestLocation(order.StaffId.Value);
            if (location == null)
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
    }
}