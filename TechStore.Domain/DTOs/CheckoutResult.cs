using TechStore.Domain.DTOs.Response;

namespace TechStore.Domain.DTOs
{
    /// <summary>
    /// Wrapper returned by CheckoutAsync.
    /// Checkout always completes without redirecting to an external payment page.
    /// </summary>
    public class CheckoutResult
    {
        /// <summary>The created order details.</summary>
        public OrderResponseDTO Order { get; set; } = null!;

    }
}
