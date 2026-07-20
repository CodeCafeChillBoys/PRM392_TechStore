using System.ComponentModel.DataAnnotations;

namespace TechStore.Domain.DTOs.Request
{
    public class VerifyOtpRequest
    {
        [Required]
        public string VerifyToken { get; set; } = string.Empty;

        [Required]
        [StringLength(6, MinimumLength = 6)]
        public string OtpCode { get; set; } = string.Empty;
    }
}
