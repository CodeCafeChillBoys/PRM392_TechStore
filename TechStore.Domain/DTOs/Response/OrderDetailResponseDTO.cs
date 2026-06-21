using System;

namespace TechStore.Domain.DTOs.Response
{
    public class OrderDetailResponseDTO
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
