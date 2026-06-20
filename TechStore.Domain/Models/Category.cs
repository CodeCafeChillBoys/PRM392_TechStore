using System.ComponentModel.DataAnnotations;
namespace TechStore.Domain.Models
{
    public class Category
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string Name { get; set; }

        public string? Description { get; set; }

        public ICollection<Product> Products { get; set; }
    }
}