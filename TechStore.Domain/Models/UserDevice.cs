using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechStore.Domain.Models
{
    public class UserDevice
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        [Required]
        public Guid UserId { get; set; }
        [Required]
        public string FcmToken { get; set; } = string.Empty;
        public string? DeviceType { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        // Navigation properties
        [ForeignKey("UserId")]
        public User User { get; set; } = null!;
    }
}