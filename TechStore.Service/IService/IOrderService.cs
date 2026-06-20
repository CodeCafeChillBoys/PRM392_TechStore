using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TechStore.Domain.DTOs.Request;
using TechStore.Domain.Models;

namespace TechStore.Service.IService
{
    public interface IOrderService
    {
        Task<IEnumerable<Order>> GetAllOrdersAsync();
        Task<Order?> GetOrderByIdAsync(Guid id);
        Task<IEnumerable<Order>> GetOrdersByUserIdAsync(Guid userId);
        Task<Order> CreateOrderAsync(CreateOrderDTO orderDto);
        Task UpdateOrderStatusAsync(Guid id, string newStatus);
        Task DeleteOrderAsync(Guid id);
    }
}
