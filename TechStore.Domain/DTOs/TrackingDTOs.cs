using System;

namespace TechStore.Domain.DTOs
{
    public class UpdateShipperLocationRequest
    {
        public Guid ShipperId { get; set; }
        public double Lat { get; set; }
        public double Lng { get; set; }
        public Guid? OrderId { get; set; }
    }

    public class TrackingLocation
    {
        public Guid ShipperId { get; set; }
        public double Lat { get; set; }
        public double Lng { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
