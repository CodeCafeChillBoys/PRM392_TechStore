using Microsoft.AspNetCore.Http;

namespace TechStore.Domain.DTOs.Request
{
    public class UploadDeliveryProofRequest
    {
        public IFormFile Image { get; set; } = null!;
    }
}
