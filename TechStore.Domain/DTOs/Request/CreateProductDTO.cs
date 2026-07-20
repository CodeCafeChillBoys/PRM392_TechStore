using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace TechStore.Domain.DTOs.Request
{
    public class CreateProductDTO
    {
        [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm")]
        public string Name { get; set; }
        [Required(ErrorMessage = "Vui lòng nhập hãng sản xuất")]
        public string Brand { get; set; }
        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Giá không hợp lệ")]
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        /// <summary>Ảnh sản phẩm upload từ máy (multipart). Không bắt buộc.</summary>
        public IFormFile? Image { get; set; }
        public string? Description { get; set; }
        [Required]
        public Guid CategoryId { get; set; }
    }
}
