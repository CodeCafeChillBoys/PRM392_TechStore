using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechStore.Domain.DTOs.Request
{
    public class CategoryDTO
    {
        [Required(ErrorMessage = "Tên danh mục là bắt buộc")]
        public string Name { get; set; }
        public string? Description { get; set; }
    }
}
