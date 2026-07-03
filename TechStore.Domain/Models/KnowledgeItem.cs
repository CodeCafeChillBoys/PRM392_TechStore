using System;
using System.ComponentModel.DataAnnotations;

namespace TechStore.Domain.Models
{
    public class KnowledgeItem
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Category { get; set; }
        
        [Required]
        public string Content { get; set; }
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
