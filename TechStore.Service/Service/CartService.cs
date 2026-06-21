using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TechStore.Domain.DTOs;
using TechStore.Domain.Models;
using TechStore.Repository.IRepositories;
using TechStore.Service.IService;

namespace TechStore.Service.Service
{
    public class CartService : ICartService
    {
        private readonly IUnitOfWork _unitOfWork;

        public CartService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<Cart>> GetCartsByUserIdAsync(Guid userId)
        {
            return await _unitOfWork.Carts.GetCartsByUserIdAsync(userId);
        }

        public async Task<Cart> AddToCartAsync(AddToCartDTO cartDto)
        {
            // Kiểm tra xem sản phẩm đã có trong giỏ hàng chưa
            var existingCartItem = await _unitOfWork.Carts.GetCartItemAsync(cartDto.UserId, cartDto.ProductId);

            if (existingCartItem != null)
            {
                // Đã có thì tăng số lượng
                existingCartItem.Quantity += cartDto.Quantity;
                _unitOfWork.Carts.Update(existingCartItem);
                await _unitOfWork.CompleteAsync();
                return existingCartItem;
            }
            else
            {
                // Chưa có thì tạo mới
                var newCartItem = new Cart
                {
                    UserId = cartDto.UserId,
                    ProductId = cartDto.ProductId,
                    Quantity = cartDto.Quantity
                };
                await _unitOfWork.Carts.AddAsync(newCartItem);
                await _unitOfWork.CompleteAsync();
                return newCartItem;
            }
        }

        public async Task UpdateCartItemQuantityAsync(Guid id, UpdateCartDTO updateDto)
        {
            var cartItem = await _unitOfWork.Carts.GetByIdAsync(id);
            if (cartItem != null)
            {
                cartItem.Quantity = updateDto.Quantity;
                _unitOfWork.Carts.Update(cartItem);
                await _unitOfWork.CompleteAsync();
            }
        }

        public async Task RemoveFromCartAsync(Guid id)
        {
            var cartItem = await _unitOfWork.Carts.GetByIdAsync(id);
            if (cartItem != null)
            {
                _unitOfWork.Carts.Remove(cartItem);
                await _unitOfWork.CompleteAsync();
            }
        }

        public async Task ClearCartAsync(Guid userId)
        {
            var cartItems = await _unitOfWork.Carts.GetCartsByUserIdAsync(userId);
            foreach (var item in cartItems)
            {
                _unitOfWork.Carts.Remove(item);
            }
            await _unitOfWork.CompleteAsync();
        }
    }
}
