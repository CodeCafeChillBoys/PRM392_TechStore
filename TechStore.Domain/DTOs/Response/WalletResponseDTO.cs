namespace TechStore.Domain.DTOs.Response
{
    public class WalletResponseDTO
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public decimal Balance { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
