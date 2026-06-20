namespace TechStore.Domain.DTOs.Respone
{
    public class UserResponse
    {
        public Guid UserId { get; set; }

        public string Username { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

    }
}