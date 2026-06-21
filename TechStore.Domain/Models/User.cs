using System.ComponentModel.DataAnnotations;
using TechStore.Domain.Enum;

namespace TechStore.Domain.Models
{
    public class User
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string FullName { get; set; }

        [Required]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        public string? PhoneNumber { get; set; }

        public string? Address { get; set; }

        [Required]
        public Role Role { get; set; } = Role.Customer;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Cart> Carts { get; set; }
        public ICollection<Order> Orders { get; set; }
    }
}