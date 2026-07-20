using TechStore.Domain.Enum;
using TechStore.Domain.Models;
using TechStore.Repository.IRepositories;
using TechStore.Service.IService;

namespace TechStore.Service.Service
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;

        public UserService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<User>> GetUsersAsync(string? role)
        {
            var users = await _unitOfWork.Users.GetAllAsync();
            if (!string.IsNullOrWhiteSpace(role) &&
                Enum.TryParse<Role>(role, ignoreCase: true, out var parsed))
            {
                users = users.Where(u => u.Role == parsed);
            }
            return users.OrderByDescending(u => u.CreatedAt);
        }

        public Task<User?> GetUserByIdAsync(Guid id) => _unitOfWork.Users.GetByIdAsync(id);

        public async Task<(User? user, string? error)> ChangeRoleAsync(Guid id, string role)
        {
            if (!Enum.TryParse<Role>(role, ignoreCase: true, out var parsed))
            {
                return (null, "Role không hợp lệ. Chỉ chấp nhận: Customer, Staff hoặc Admin.");
            }
            var user = await _unitOfWork.Users.GetByIdAsync(id);
            if (user == null) return (null, "Không tìm thấy người dùng.");

            user.Role = parsed;
            _unitOfWork.Users.Update(user);
            await _unitOfWork.CompleteAsync();
            return (user, null);
        }
    }
}
