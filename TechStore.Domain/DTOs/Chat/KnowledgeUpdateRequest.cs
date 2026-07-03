using System.ComponentModel.DataAnnotations;

namespace TechStore.Domain.DTOs.Chat
{
    public class KnowledgeUpdateRequest
    {
        [Required]
        public string Category { get; set; }
        
        [Required]
        public string Content { get; set; }
    }
}
