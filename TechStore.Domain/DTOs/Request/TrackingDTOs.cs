namespace TechStore.Domain.DTOs.Request
{
    public class TrackingDTOs
    {
        public class UpdateLocationRequest
        {
            public Guid ShipperId { get; set; }
            public double Lat { get; set; }
            public double Lng { get; set; }
        }

        public class TrackingLocation
        {
            public Guid ShipperId { get; set; }
            public double Lat { get; set; }
            public double Lng { get; set; }
            public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        }
    }
}