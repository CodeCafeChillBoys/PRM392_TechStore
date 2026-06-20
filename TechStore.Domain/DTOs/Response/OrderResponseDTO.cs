using System;
using System.Collections.Generic;

namespace TechStore.Domain.DTOs.Response
{
    public class OrderResponseDTO
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string ShippingAddress { get; set; }
        public string PaymentMethod { get; set; }
        public string Status { get; set; }
        public List<OrderDetailResponseDTO> OrderDetails { get; set; }
    }
}
