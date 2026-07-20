using System;

namespace TechStore.Domain.DTOs.Response
{
    /// <summary>
    /// Phien dang nhap cho khu giam sat van hanh (Admin).
    /// KHONG lo OtpCode / AccessToken / RefreshToken / FcmToken.
    /// </summary>
    public class LoginSessionInfoDTO
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsOtpSent { get; set; }
        public string? DeviceId { get; set; }
        public string? DeviceName { get; set; }
        public string? DeviceType { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiredAt { get; set; }
    }
}
