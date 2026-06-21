using System;

namespace TechStore.Domain.Models
{
    public class LoginSession
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid UserId { get; set; }

        public string VerifyToken { get; set; } = string.Empty;

        public string OtpCode { get; set; } = string.Empty;

        public string Status { get; set; } = "Pending"; // "Pending", "Approved", "Expired"

        public bool IsOtpSent { get; set; }

        public string? AccessToken { get; set; }

        public string? RefreshToken { get; set; }

        public int ExpiresIn { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime ExpiredAt { get; set; }

        // Navigation property
        public User User { get; set; } = null!;
    }
}
