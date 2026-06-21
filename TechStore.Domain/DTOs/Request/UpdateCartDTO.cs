using System;
using System.ComponentModel.DataAnnotations;

namespace TechStore.Domain.DTOs.Request
{
    public class UpdateCartDTO
    {
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
        public int Quantity { get; set; }
    }
}
