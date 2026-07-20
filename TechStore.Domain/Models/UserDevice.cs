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
        [MaxLength(255)]
        public string DeviceId { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? DeviceName { get; set; }

        [Required]
        public string FcmToken { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? DeviceType { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;
    }
}