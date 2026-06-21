using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TechStore.Domain.DTOs;

namespace TechStore.Service.IServices
{
    public interface IOrderService
    {
        /// <summary>
        /// Converts the current user's cart into a confirmed order.
        /// - COD/BankTransfer/CreditCard: validates, creates order, deducts stock, clears cart → returns CheckoutResult.Order only.
        /// - VNPay: validates, creates order (PendingPayment), deducts stock, clears cart → returns CheckoutResult with PaymentUrl.
        /// </summary>
        Task<CheckoutResult> CheckoutAsync(CheckoutRequest request, string ipAddress);

        /// <summary>
        /// Returns all orders placed by the specified user, newest first.
        /// </summary>
        Task<List<OrderResponse>> GetOrdersByUserAsync(Guid userId);

        /// <summary>
        /// Returns a single order by its ID, or null if not found.
        /// </summary>
        Task<OrderResponse?> GetOrderByIdAsync(Guid orderId);

        /// <summary>
        /// Updates the status of an order (e.g., Pending → Shipped → Delivered).
        /// </summary>
        Task<OrderResponse?> UpdateOrderStatusAsync(Guid orderId, string newStatus);

        /// <summary>
        /// Called by the VNPay IPN/Return handler to finalize payment.
        /// Sets PaymentStatus=Paid + Status=Confirmed on success,
        /// or PaymentStatus=Failed + Status=Cancelled on failure.
        /// Idempotent — safe to call multiple times.
        /// </summary>
        Task<bool> ConfirmVnpayPaymentAsync(Guid orderId, bool success, string transactionId);
    }
}
