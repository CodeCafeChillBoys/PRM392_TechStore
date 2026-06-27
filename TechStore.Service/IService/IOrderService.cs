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

        // ── Checkout + VNPay (billing) ───────────────────────────────────────

        /// <summary>
        /// Converts the current user's cart into a confirmed order.
        /// - COD/BankTransfer/CreditCard: validates, creates order, deducts stock, clears cart → returns CheckoutResult.Order only.
        /// - VNPay: validates, creates order (PendingPayment), deducts stock, clears cart → returns CheckoutResult with PaymentUrl.
        /// </summary>
        Task<CheckoutResult> CheckoutAsync(CheckoutRequest request, string ipAddress);

        /// <summary>
        /// Called by the VNPay IPN/Return handler to finalize payment.
        /// Sets PaymentStatus=Paid + Status=Confirmed on success,
        /// or PaymentStatus=Failed + Status=Cancelled on failure.
        /// Idempotent — safe to call multiple times.
        /// </summary>
        Task<bool> ConfirmVnpayPaymentAsync(Guid orderId, bool success, string transactionId);


        Task<IEnumerable<Guid>> GetActiveOrderIdsByShipperAsync(Guid shipperId);
        Task<bool> ConfirmDeliveryAsync(Guid orderId, IFormFile imageFile);
        Task AssignShipperAsync(Guid orderId, Guid staffId);
    }
}
