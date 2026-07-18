using System.ComponentModel.DataAnnotations;

namespace TechStore.Domain.DTOs.Request
{
    public class UpdateStockRequest
    {
        [Range(0, int.MaxValue, ErrorMessage = "Tồn kho không được âm.")]
        public int StockQuantity { get; set; }
    }
}
