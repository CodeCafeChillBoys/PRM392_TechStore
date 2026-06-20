using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TechStore.Domain.DTOs.Request;
using TechStore.Domain.Models;
using TechStore.Repository.IRepositories;
using TechStore.Service.IService;

namespace TechStore.Service.Service
{
    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork _unitOfWork;

        public OrderService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<Order>> GetAllOrdersAsync()
        {
            return await _unitOfWork.Orders.GetOrdersWithDetailsAsync();
        }

        public async Task<Order?> GetOrderByIdAsync(Guid id)
        {
            return await _unitOfWork.Orders.GetOrderByIdWithDetailsAsync(id);
        }

        public async Task<IEnumerable<Order>> GetOrdersByUserIdAsync(Guid userId)
        {
            return await _unitOfWork.Orders.GetOrdersByUserIdAsync(userId);
        }

        public async Task<Order> CreateOrderAsync(CreateOrderDTO orderDto)
        {
            var order = new Order
            {
                UserId = orderDto.UserId,
                ShippingAddress = orderDto.ShippingAddress,
                PaymentMethod = orderDto.PaymentMethod,
                OrderDate = DateTime.UtcNow,
                Status = "Pending",
                OrderDetails = new List<OrderDetail>()
            };

            decimal totalAmount = 0;

            foreach (var detail in orderDto.OrderDetails)
            {
                var orderDetail = new OrderDetail
                {
                    ProductId = detail.ProductId,
                    Quantity = detail.Quantity,
                    UnitPrice = detail.UnitPrice
                };
                totalAmount += detail.Quantity * detail.UnitPrice;
                order.OrderDetails.Add(orderDetail);
            }

            order.TotalAmount = totalAmount;

            await _unitOfWork.Orders.AddAsync(order);
            await _unitOfWork.CompleteAsync();

            return order;
        }

        public async Task UpdateOrderStatusAsync(Guid id, string newStatus)
        {
            var order = await _unitOfWork.Orders.GetByIdAsync(id);
            if (order != null)
            {
                order.Status = newStatus;
                _unitOfWork.Orders.Update(order);
                await _unitOfWork.CompleteAsync();
            }
        }

        public async Task DeleteOrderAsync(Guid id)
        {
            var order = await _unitOfWork.Orders.GetOrderByIdWithDetailsAsync(id);
            if (order != null)
            {
                _unitOfWork.Orders.Remove(order);
                await _unitOfWork.CompleteAsync();
            }
        }
    }
}
