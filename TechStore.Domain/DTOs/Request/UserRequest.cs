using TechStore.Domain.Enum;

namespace TechStore.Domain.DTOs.Request
{
    public class UserRequest
    {
        public Guid Id { get; set; }
        public string? Email { get; set; }
        public string? Name { get; set; }
        public string? PhoneNumber { get; set; }
        public Role Role { get; set; }
    }
}