using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using TechStore.Domain.DTOs;
using TechStore.Domain.DTOs.Request;
using TechStore.Domain.Models;

namespace TechStore.Service.IService
{
    public interface IOrderService
    {
        // ── CRUD (develop) ───────────────────────────────────────────────────
        Task<IEnumerable<Order>> GetAllOrdersAsync();
        Task<Order?> GetOrderByIdAsync(Guid id);
        Task<IEnumerable<Order>> GetOrdersByUserIdAsync(Guid userId);
        Task<Order> CreateOrderAsync(CreateOrderDTO orderDto);
        Task UpdateOrderStatusAsync(Guid id, string newStatus);
        Task DeleteOrderAsync(Guid id);

        // ── Checkout ─────────────────────────────────────────────────────────

        /// <summary>
        /// Converts the current user's cart into a confirmed order.
        /// Wallet payments debit the authenticated user's wallet in the same database transaction
        /// that creates the order, deducts stock and clears the cart.
        /// </summary>
        Task<CheckoutResult> CheckoutAsync(CheckoutRequest request);

        // lấy lên đơn hàng đang giao vs shipperID 
        Task<IEnumerable<Guid>> GetActiveOrderIdsByShipperAsync(Guid shipperId);
        // Update order đã giao và hình ảnh
        Task<bool> ConfirmDeliveryAsync(Guid orderId, IFormFile imageFile);
        // gán đơn hàng cho shipperId
        Task AssignShipperAsync(Guid orderId, Guid staffId);
    }
}
