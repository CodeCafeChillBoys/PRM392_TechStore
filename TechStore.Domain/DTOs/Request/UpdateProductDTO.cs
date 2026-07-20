using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace TechStore.Domain.DTOs.Request
{
    /// <summary>
    /// Body của PUT /api/Products/{id} (multipart). Giống CreateProductDTO;
    /// Image không gửi = giữ ảnh cũ.
    /// </summary>
    public class UpdateProductDTO
    {
        [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm")]
        public string Name { get; set; }
        [Required(ErrorMessage = "Vui lòng nhập hãng sản xuất")]
        public string Brand { get; set; }
        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Giá không hợp lệ")]
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public IFormFile? Image { get; set; }
        public string? Description { get; set; }
        [Required]
        public Guid CategoryId { get; set; }
    }
}
