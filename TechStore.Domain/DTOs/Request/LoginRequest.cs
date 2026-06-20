

namespace TechStore.Domain.DTOs.Request
{
    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;

        public string PassWord { get; set; } = string.Empty;
    }
}