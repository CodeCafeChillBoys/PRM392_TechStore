using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TechStore.Domain.Models;

namespace TechStore.Repository.IRepositories
{
    public interface ICartRepository : IGenericRepository<Cart>
    {
        Task<IEnumerable<Cart>> GetCartsByUserIdAsync(Guid userId);
        Task<Cart?> GetCartItemAsync(Guid userId, Guid productId);
    }
}
