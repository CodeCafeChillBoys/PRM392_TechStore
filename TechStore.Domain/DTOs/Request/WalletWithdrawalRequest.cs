using System.ComponentModel.DataAnnotations;

namespace TechStore.Domain.DTOs.Request
{
    public class WalletWithdrawalRequest
    {
        [Range(typeof(decimal), "50000", "1000000000", ErrorMessage = "Số tiền rút tối thiểu là 50.000đ.")]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(100)]
        public string BankName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string BankAccountNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string AccountHolderName { get; set; } = string.Empty;
    }
}
