
namespace TechStore.Domain.DTOs.Respone
{
    public class ApiResponse<T>
    {
        public bool success { get; set; }
        public string? message { get; set; }

        public T? Data { get; set; }

        public object? Errors { get; set; }
    }
}