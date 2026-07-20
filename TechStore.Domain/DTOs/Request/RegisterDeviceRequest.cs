using System.ComponentModel.DataAnnotations;

namespace TechStore.Domain.DTOs.Request
{
    public class RegisterDeviceRequest
    {
        public string DeviceId { get; set; } = string.Empty;

        public string DeviceName { get; set; } = string.Empty;

        public string DeviceType { get; set; } = string.Empty;

        public string FcmToken { get; set; } = string.Empty;
    }
}