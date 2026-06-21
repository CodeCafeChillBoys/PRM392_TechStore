using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TechStore.Domain.DTOs;
using TechStore.Service.IService;

namespace TechStoreAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartsController : ControllerBase
    {
        private readonly ICartService _cartService;
        private readonly IMapper _mapper;

        public CartsController(ICartService cartService, IMapper mapper)
        {
            _cartService = cartService;
            _mapper = mapper;
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<CartResponseDTO>>> GetCartByUser(Guid userId)
        {
            var carts = await _cartService.GetCartsByUserIdAsync(userId);
            var response = _mapper.Map<IEnumerable<CartResponseDTO>>(carts);
            return Ok(response);
        }

        [HttpPost("add")]
        public async Task<ActionResult<CartResponseDTO>> AddToCart(AddToCartDTO cartDto)
        {
            var cartItem = await _cartService.AddToCartAsync(cartDto);
            
            // Lấy lại để có thông tin Product map ra DTO cho đầy đủ
            var carts = await _cartService.GetCartsByUserIdAsync(cartDto.UserId);
            var updatedItem = default(TechStore.Domain.Models.Cart);
            foreach(var item in carts) {
                if (item.Id == cartItem.Id) {
                    updatedItem = item;
                    break;
                }
            }
            
            var response = _mapper.Map<CartResponseDTO>(updatedItem ?? cartItem);
            return Ok(response);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCartQuantity(Guid id, UpdateCartDTO updateDto)
        {
            await _cartService.UpdateCartItemQuantityAsync(id, updateDto);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> RemoveFromCart(Guid id)
        {
            await _cartService.RemoveFromCartAsync(id);
            return NoContent();
        }

        [HttpDelete("clear/{userId}")]
        public async Task<IActionResult> ClearCart(Guid userId)
        {
            await _cartService.ClearCartAsync(userId);
            return NoContent();
        }
    }
}
