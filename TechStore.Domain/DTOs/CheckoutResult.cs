using TechStore.Domain.DTOs.Response;

namespace TechStore.Domain.DTOs
{
    /// <summary>
    /// Wrapper returned by CheckoutAsync.
    /// For COD/BankTransfer/CreditCard: only Order is populated.
    /// For VNPay: Order + PaymentUrl are both populated.
    /// </summary>
    public class CheckoutResult
    {
        /// <summary>The created order details.</summary>
        public OrderResponseDTO Order { get; set; } = null!;

        /// <summary>
        /// VNPay payment URL — non-null only when PaymentMethod is "VNPay".
        /// Mobile client should open this URL in a WebView.
        /// </summary>
        public string? PaymentUrl { get; set; }

        /// <summary>True when the client must redirect to complete payment.</summary>
        public bool RequiresOnlinePayment => PaymentUrl != null;
    }
}
