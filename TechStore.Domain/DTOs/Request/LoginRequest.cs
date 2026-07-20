

namespace TechStore.Domain.DTOs.Request
{
    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;

        public string PassWord { get; set; } = string.Empty;

        public string? DeviceId { get; set; }

        public string? DeviceName { get; set; }

        public string? DeviceType { get; set; }

        public string? FcmToken { get; set; }
    }
}