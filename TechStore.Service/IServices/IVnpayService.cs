using Microsoft.AspNetCore.Http;
using TechStore.Domain.Models;

namespace TechStore.Service.IServices
{
    public interface IVnpayService
    {
        /// <summary>
        /// Builds a signed VNPay payment URL for the given order.
        /// </summary>
        /// <param name="order">The order to pay for.</param>
        /// <param name="ipAddress">Client IP address (required by VNPay).</param>
        /// <returns>Full payment URL including HMAC-SHA512 signature.</returns>
        string CreatePaymentUrl(Order order, string ipAddress);

        /// <summary>
        /// Validates the HMAC-SHA512 signature on the VNPay callback query string.
        /// Pass Request.QueryString.Value (the RAW, URL-encoded query string) so the
        /// hash is computed over the same encoded representation VNPay uses.
        /// </summary>
        /// <param name="rawQueryString">Raw query string from Request.QueryString.Value (e.g. "?vnp_Amount=...&vnp_SecureHash=...").</param>
        /// <param name="responseCode">vnp_ResponseCode: "00" = success.</param>
        /// <param name="transactionId">vnp_TransactionNo returned by VNPay.</param>
        /// <param name="orderId">vnp_TxnRef = our Order.Id as string.</param>
        /// <returns>True if the signature is valid (not tampered).</returns>
        bool ValidateSignature(
            string rawQueryString,
            out string responseCode,
            out string transactionId,
            out string orderId);
    }
}
