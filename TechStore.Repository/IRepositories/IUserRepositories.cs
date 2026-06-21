using TechStore.Domain.Models;

namespace TechStore.Repository.IRepositories
{
    public interface IUserRepositories : IGenericRepository<User>
    {
        Task<User?> GetByUsernameAsync(string username);
        Task<User?> GetByEmailAsync(string email);

    }
}