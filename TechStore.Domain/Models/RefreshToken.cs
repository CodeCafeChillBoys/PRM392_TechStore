namespace TechStore.Domain.Models
{
    public class RefreshToken
    {
        public int RefreshTokenId { get; set; }

        public int UserId { get; set; }

        public string Token { get; set; } = string.Empty;

        public DateTime ExpiryDate { get; set; }

        public bool IsRevoked { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public User User { get; set; } = null!;

    }
}