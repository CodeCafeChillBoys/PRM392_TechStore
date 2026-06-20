using System;

namespace TechStore.Domain.DTOs.Request
{
    public class CreateOrderDetailDTO
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
