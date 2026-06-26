namespace TechStore.Domain.DTOs.Response
{
    public class NotificationFeedsResponseDTO
    {
        public List<NotificationResponseDTO> Promo { get; set; } = new();
        public List<NotificationResponseDTO> Orders { get; set; } = new();
    }
}
