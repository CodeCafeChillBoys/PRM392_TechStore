using System;

namespace TechStore.Domain.DTOs
{
    public class CartResponseDTO
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public string? ProductImageUrl { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice { get; set; } // Quantity * UnitPrice
    }
}
