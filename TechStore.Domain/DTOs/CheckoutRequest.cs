using System;
using System.ComponentModel.DataAnnotations;

namespace TechStore.Domain.DTOs
{
    public class CheckoutRequest
    {
        [Required(ErrorMessage = "Shipping address is required.")]
        public string ShippingAddress { get; set; } = string.Empty;

        /// <summary>
        /// Accepted values: "COD", "BankTransfer", "CreditCard", "Wallet".
        /// VNPay is only used to top up the wallet.
        /// </summary>
        [Required(ErrorMessage = "Payment method is required.")]
        [RegularExpression("^(COD|BankTransfer|CreditCard|Wallet)$",
            ErrorMessage = "PaymentMethod must be one of: COD, BankTransfer, CreditCard, Wallet.")]
        public string PaymentMethod { get; set; } = "COD";

        
        [Required]
        public Guid UserId { get; set; }

        /// <summary>Phí ship do FE tính và gửi lên, cộng vào tổng tiền đơn hàng.</summary>
        public decimal ShippingFee { get; set; } = 0;
    }
}
