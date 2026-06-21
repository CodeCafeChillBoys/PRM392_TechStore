using System;
using System.ComponentModel.DataAnnotations;

namespace TechStore.Domain.DTOs
{
    public class CheckoutRequest
    {
        [Required(ErrorMessage = "Shipping address is required.")]
        public string ShippingAddress { get; set; } = string.Empty;

        /// <summary>
        /// Accepted values: "COD", "BankTransfer", "CreditCard"
        /// </summary>
        [Required(ErrorMessage = "Payment method is required.")]
        [RegularExpression("^(COD|BankTransfer|CreditCard|VNPay)$",
            ErrorMessage = "PaymentMethod must be one of: COD, BankTransfer, CreditCard, VNPay.")]
        public string PaymentMethod { get; set; } = "COD";

        /// <summary>
        /// The ID of the user placing the order.
        /// (Will be replaced by JWT claim once auth is added.)
        /// </summary>
        [Required]
        public Guid UserId { get; set; }
    }
}
