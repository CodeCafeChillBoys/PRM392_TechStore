using System;

namespace TechStore.Domain.DTOs.Response
{
    /// <summary>
    /// Thiet bi cua user cho khu giam sat van hanh (Admin). KHONG lo FcmToken.
    /// </summary>
    public class DeviceInfoDTO
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string DeviceId { get; set; } = string.Empty;
        public string? DeviceName { get; set; }
        public string? DeviceType { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
