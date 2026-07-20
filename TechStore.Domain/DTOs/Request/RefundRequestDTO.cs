using Microsoft.AspNetCore.Http;

namespace TechStore.Domain.DTOs.Request
{
    /// <summary>
    /// Yêu cầu hoàn tiền do khách gửi cho một đơn đã giao (Delivered + Paid).
    /// </summary>
    public class RefundRequestDTO
    {
        public string Reason { get; set; } = string.Empty;
        public IFormFile? Image { get; set; }
    }
}
