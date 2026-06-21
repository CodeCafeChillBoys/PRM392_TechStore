using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

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

        /// <summary>
        /// VNPay transaction number (vnp_TransactionNo) — null for COD/BankTransfer.
        /// </summary>
        public string? VnpayTransactionId { get; set; }

        public User User { get; set; }

        public ICollection<OrderDetail> OrderDetails { get; set; }
    }
}