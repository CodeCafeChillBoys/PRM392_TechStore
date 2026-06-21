using System.ComponentModel.DataAnnotations;

namespace TechStore.Domain.DTOs.Request
{
    public class RegisterDeviceRequest
    {
        [Required]
        public string FcmToken { get; set; } = string.Empty;
        public string? DeviceType { get; set; }
    }
}