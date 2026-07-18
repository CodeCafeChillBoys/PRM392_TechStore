using System.ComponentModel.DataAnnotations;

namespace TechStore.Domain.Models
{
    public class Order
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid UserId { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        public decimal TotalAmount { get; set; }

        public string ShippingAddress { get; set; }

        public string PaymentMethod { get; set; }

        public string Status { get; set; } = "Pending";

        /// <summary>
        /// Payment status independent of order status.
        /// Values: Pending | Paid | Failed | Cancelled
        /// </summary>
        public string PaymentStatus { get; set; } = "Pending";

      
        public string? VnpayTransactionId { get; set; }

        public User User { get; set; }

        public ICollection<OrderDetail> OrderDetails { get; set; }

        public Guid? StaffId { get; set; }
        public string? DeliveryProofImageUrl { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        /// <summary>
        /// Phi van chuyen cua don (khach tra) — nen tang tinh hoa hong shipper.
        /// Khop migration AddShippingFeeAndVehicleToOrder (numeric, default 0).
        /// </summary>
        public decimal ShippingFee { get; set; }

        /// <summary>
        /// Loai xe shipper giao don nay (nullable — chua gan khi moi tao don).
        /// </summary>
        public string? ShipperVehicle { get; set; }
    }
}