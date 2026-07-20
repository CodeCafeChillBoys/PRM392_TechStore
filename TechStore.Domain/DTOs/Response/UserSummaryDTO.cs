using System;

namespace TechStore.Domain.DTOs.Response
{
    /// <summary>
    /// Thong tin user cho khu Admin — KHONG BAO GIO chua PasswordHash.
    /// </summary>
    public class UserSummaryDTO
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string Role { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
