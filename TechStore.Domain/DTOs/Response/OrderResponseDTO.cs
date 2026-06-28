using System;
using System.Collections.Generic;

namespace TechStore.Domain.DTOs.Response
{
    public class OrderResponseDTO
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string ShippingAddress { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public string? VnpayTransactionId { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public List<OrderDetailResponseDTO> OrderDetails { get; set; } = new();
    }
}
