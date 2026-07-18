using System.ComponentModel.DataAnnotations;

namespace TechStore.Domain.DTOs.Request
{
    public class WalletTopUpRequest
    {
        [Range(typeof(decimal), "1", "1000000000", ErrorMessage = "Số tiền nạp phải lớn hơn 0.")]
        public decimal Amount { get; set; }
    }
}
