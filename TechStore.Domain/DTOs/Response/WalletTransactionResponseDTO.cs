using System.Text.Json.Serialization;
using TechStore.Domain.Enum;

namespace TechStore.Domain.DTOs.Response
{
    public class WalletTransactionResponseDTO
    {
        public Guid Id { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public WalletTransactionType Type { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public WalletTransactionStatus Status { get; set; }
        public decimal Amount { get; set; }
        public decimal? BalanceBefore { get; set; }
        public decimal? BalanceAfter { get; set; }
        public string? Description { get; set; }
        public string? VnpayTransactionId { get; set; }
        public Guid? OrderId { get; set; }
        public string? BankName { get; set; }
        public string? BankAccountNumber { get; set; }
        public string? AccountHolderName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
    }
}
