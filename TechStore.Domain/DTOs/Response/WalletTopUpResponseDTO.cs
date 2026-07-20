namespace TechStore.Domain.DTOs.Response
{
    public class WalletTopUpResponseDTO
    {
        public Guid TransactionId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentUrl { get; set; } = string.Empty;
    }
}
