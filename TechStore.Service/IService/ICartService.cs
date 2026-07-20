using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TechStore.Domain.DTOs.Request;
using TechStore.Domain.Models;

namespace TechStore.Service.IService
{
    public interface ICartService
    {
        Task<IEnumerable<Cart>> GetCartsByUserIdAsync(Guid userId);
        Task<Cart> AddToCartAsync(AddToCartDTO cartDto);
        Task UpdateCartItemQuantityAsync(Guid id, UpdateCartDTO updateDto);
        Task RemoveFromCartAsync(Guid id);
        Task ClearCartAsync(Guid userId);
    }
}
