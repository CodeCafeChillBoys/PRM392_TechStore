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

        public User User { get; set; }

        public ICollection<OrderDetail> OrderDetails { get; set; }
    }
}