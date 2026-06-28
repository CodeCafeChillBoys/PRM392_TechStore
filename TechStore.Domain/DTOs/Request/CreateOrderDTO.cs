using System;
using System.Collections.Generic;

namespace TechStore.Domain.DTOs.Request
{
    public class CreateOrderDTO
    {
        public Guid UserId { get; set; }
        public string ShippingAddress { get; set; }
        public string PaymentMethod { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public List<CreateOrderDetailDTO> OrderDetails { get; set; } = new List<CreateOrderDetailDTO>();
    }
}
