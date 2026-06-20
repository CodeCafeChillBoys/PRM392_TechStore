using System.ComponentModel.DataAnnotations;

namespace TechStore.Domain.Models
{
    public class Message
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid SenderId { get; set; }

        public Guid? ReceiverId { get; set; }

        public string MessageText { get; set; }

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        public User Sender { get; set; }
    }
}