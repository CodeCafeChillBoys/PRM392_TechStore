using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechStore.Domain.DTOs.Request;
using TechStore.Service.IService;

namespace TechStoreAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/devices")]
    public class DeviceController : ControllerBase
    {
        private readonly IDeviceService _deviceService;
        private readonly IFirebaseNotificationService _notificationService;

        public DeviceController(IDeviceService deviceService, IFirebaseNotificationService notificationService)
        {
            _deviceService = deviceService;
            _notificationService = notificationService;
        }

        [HttpPost("register-token")]
        public async Task<IActionResult> RegisterToken([FromBody] RegisterDeviceRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Không xác thực được người dùng." });
            }

            await _deviceService.RegisterDeviceTokenAsync(userId, request);
            return Ok(new { message = "Đăng ký FCM Token thành công." });
        }

        [AllowAnonymous]
        [HttpGet("test-send")]
        public async Task<IActionResult> TestSend([FromQuery] string token)
        {
            // Inject INotificationService vào và gọi gửi thử
            await _notificationService.SendNotificationAsync(token, "Test Title", "Test Body");
            return Ok("Đã gọi lệnh gửi, check log console backend!");
        }

    }
}
