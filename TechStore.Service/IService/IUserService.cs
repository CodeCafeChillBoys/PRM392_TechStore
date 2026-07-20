using TechStore.Domain.Models;

namespace TechStore.Service.IService
{
    public interface IUserService
    {
        Task<IEnumerable<User>> GetUsersAsync(string? role);
        Task<User?> GetUserByIdAsync(Guid id);

        /// <summary>Doi role. Tra ve (user, errorMessage). user=null neu khong tim thay/role sai.</summary>
        Task<(User? user, string? error)> ChangeRoleAsync(Guid id, string role);
    }
}
