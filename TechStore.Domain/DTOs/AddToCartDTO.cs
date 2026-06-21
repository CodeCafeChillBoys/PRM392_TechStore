using System;
using System.ComponentModel.DataAnnotations;

namespace TechStore.Domain.DTOs
{
    public class AddToCartDTO
    {
        [Required]
        public Guid UserId { get; set; }

        [Required]
        public Guid ProductId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
        public int Quantity { get; set; } = 1;
    }
}
