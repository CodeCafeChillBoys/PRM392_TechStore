namespace TechStore.Domain.DTOs.Request
{
    public class GoogleLoginRequest
    {
        public string IdToken { get; set; } = string.Empty;
        public string? DeviceId { get; set; }
        public string? DeviceName { get; set; }
        public string? DeviceType { get; set; }
        public string? FcmToken { get; set; }
    }
}