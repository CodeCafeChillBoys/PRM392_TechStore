using TechStore.Domain.DTOs.Response;
using TechStore.Domain.Models;

namespace TechStore.Service.IService
{
    public interface IAdminService
    {
        Task<AdminStatsResponse> GetStatsAsync(DateTime from, DateTime to);
        Task<IEnumerable<LoginSession>> GetSessionsAsync();
        Task<IEnumerable<UserDevice>> GetDevicesAsync();
        Task<IEnumerable<Product>> GetLowStockAsync(int threshold);

        /// <summary>Cap nhat ton kho. Tra ve product da cap nhat, null neu khong tim thay.</summary>
        Task<Product?> UpdateProductStockAsync(Guid id, int stock);
    }
}
