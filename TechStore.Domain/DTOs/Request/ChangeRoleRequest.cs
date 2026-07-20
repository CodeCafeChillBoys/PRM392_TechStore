using System.ComponentModel.DataAnnotations;

namespace TechStore.Domain.DTOs.Request
{
    /// <summary>
    /// Doi role cho user (Admin thao tac qua PUT /api/users/{id}/role).
    /// </summary>
    public class ChangeRoleRequest
    {
        [Required]
        [RegularExpression("^(Customer|Staff|Admin)$",
            ErrorMessage = "Role chỉ chấp nhận: Customer, Staff hoặc Admin.")]
        public string Role { get; set; } = string.Empty;
    }
}
