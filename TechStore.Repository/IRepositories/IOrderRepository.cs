using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TechStore.Domain.Models;

namespace TechStore.Repository.IRepositories
{
    public interface IOrderRepository : IGenericRepository<Order>
    {
        Task<IEnumerable<Order>> GetOrdersWithDetailsAsync();
        Task<Order?> GetOrderByIdWithDetailsAsync(Guid id);
        Task<IEnumerable<Order>> GetOrdersByUserIdAsync(Guid userId);
    }
}
