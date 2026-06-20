using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TechStore.Domain.DTOs.Request;
using TechStore.Domain.DTOs.Response;
using TechStore.Service.IService;

namespace TechStoreAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly IMapper _mapper;

        public OrdersController(IOrderService orderService, IMapper mapper)
        {
            _orderService = orderService;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<OrderResponseDTO>>> GetOrders()
        {
            var orders = await _orderService.GetAllOrdersAsync();
            var response = _mapper.Map<IEnumerable<OrderResponseDTO>>(orders);
            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<OrderResponseDTO>> GetOrder(Guid id)
        {
            var order = await _orderService.GetOrderByIdAsync(id);
            if (order == null) return NotFound("Đơn hàng không tồn tại");

            var response = _mapper.Map<OrderResponseDTO>(order);
            return Ok(response);
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<OrderResponseDTO>>> GetOrdersByUser(Guid userId)
        {
            var orders = await _orderService.GetOrdersByUserIdAsync(userId);
            var response = _mapper.Map<IEnumerable<OrderResponseDTO>>(orders);
            return Ok(response);
        }

        [HttpPost]
        public async Task<ActionResult<OrderResponseDTO>> CreateOrder(CreateOrderDTO createOrderDto)
        {
            if (createOrderDto.OrderDetails == null || createOrderDto.OrderDetails.Count == 0)
            {
                return BadRequest("Đơn hàng phải có ít nhất 1 sản phẩm.");
            }

            var createdOrder = await _orderService.CreateOrderAsync(createOrderDto);
            
            // Lấy lại order với details đầy đủ để map cho đẹp
            var orderWithDetails = await _orderService.GetOrderByIdAsync(createdOrder.Id);
            var response = _mapper.Map<OrderResponseDTO>(orderWithDetails);

            return Ok(response);
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateOrderStatus(Guid id, [FromBody] string newStatus)
        {
            var existingOrder = await _orderService.GetOrderByIdAsync(id);
            if (existingOrder == null) return NotFound("Đơn hàng không tồn tại");

            await _orderService.UpdateOrderStatusAsync(id, newStatus);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(Guid id)
        {
            var existingOrder = await _orderService.GetOrderByIdAsync(id);
            if (existingOrder == null) return NotFound("Đơn hàng không tồn tại");

            await _orderService.DeleteOrderAsync(id);
            return NoContent();
        }
    }
}
