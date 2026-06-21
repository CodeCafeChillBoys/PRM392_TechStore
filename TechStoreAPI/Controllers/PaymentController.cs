using System;
using Microsoft.AspNetCore.Mvc;
using TechStore.Service.IServices;

namespace TechStoreAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly IVnpayService _vnpayService;
        private readonly IOrderService _orderService;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(
            IVnpayService vnpayService,
            IOrderService orderService,
            ILogger<PaymentController> logger)
        {
            _vnpayService = vnpayService;
            _orderService = orderService;
            _logger = logger;
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /api/payment/vnpay-return
        // VNPay redirects the USER's browser here after payment.
        // Purpose: Return JSON so mobile FE can show success/failure screen.
        // NOTE: Do NOT rely solely on this to update DB — use IPN (below).
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("vnpay-return")]
        public async Task<IActionResult> VnpayReturn()
        {
            var isValidSignature = _vnpayService.ValidateSignature(
                Request.Query,
                out var responseCode,
                out var transactionId,
                out var orderIdStr);

            if (!isValidSignature)
            {
                _logger.LogWarning("VNPay Return: invalid signature. Query: {Query}", Request.QueryString);
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid payment signature. Possible tampering detected."
                });
            }

            bool isPaid = responseCode == "00";
            _logger.LogInformation(
                "VNPay Return — OrderId: {OrderId}, ResponseCode: {Code}, TxnId: {TxnId}, Paid: {IsPaid}",
                orderIdStr, responseCode, transactionId, isPaid);

            // Attempt to parse orderId (may fail if tampered)
            if (!Guid.TryParse(orderIdStr, out var orderId))
                return BadRequest(new { success = false, message = "Invalid order reference." });

            // Update DB as a fallback (IPN is primary, this is secondary)
            await _orderService.ConfirmVnpayPaymentAsync(orderId, isPaid, transactionId);

            // Return JSON for mobile app WebView to detect and handle
            return Ok(new
            {
                success         = isPaid,
                orderId         = orderIdStr,
                transactionId   = transactionId,
                responseCode    = responseCode,
                message         = isPaid
                    ? "Payment successful! Your order has been confirmed."
                    : $"Payment failed (code: {responseCode}). Your order has been cancelled."
            });
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /api/payment/vnpay-ipn
        // VNPay server calls this endpoint directly (server-to-server).
        // This is the AUTHORITATIVE source — always update DB here.
        // Must respond within 5 seconds with specific JSON format.
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("vnpay-ipn")]
        public async Task<IActionResult> VnpayIpn()
        {
            // VNPay expects these exact JSON responses
            const string rspCodeOk      = "00";
            const string rspCodeInvalid = "97"; // Invalid checksum
            const string rspCodeNotFound = "01"; // Order not found
            const string rspCodeError   = "99"; // Unknown error

            try
            {
                var isValidSignature = _vnpayService.ValidateSignature(
                    Request.Query,
                    out var responseCode,
                    out var transactionId,
                    out var orderIdStr);

                if (!isValidSignature)
                {
                    _logger.LogWarning("VNPay IPN: invalid signature from IP {IP}", HttpContext.Connection.RemoteIpAddress);
                    return Ok(new { RspCode = rspCodeInvalid, Message = "Invalid Checksum" });
                }

                if (!Guid.TryParse(orderIdStr, out var orderId))
                {
                    _logger.LogWarning("VNPay IPN: unparseable orderId '{OrderId}'", orderIdStr);
                    return Ok(new { RspCode = rspCodeNotFound, Message = "Order not found" });
                }

                bool isPaid = responseCode == "00";
                var updated = await _orderService.ConfirmVnpayPaymentAsync(orderId, isPaid, transactionId);

                if (!updated)
                {
                    _logger.LogWarning("VNPay IPN: order {OrderId} not found in DB", orderId);
                    return Ok(new { RspCode = rspCodeNotFound, Message = "Order not found" });
                }

                _logger.LogInformation(
                    "VNPay IPN processed — OrderId: {OrderId}, TxnId: {TxnId}, Paid: {IsPaid}",
                    orderId, transactionId, isPaid);

                // Must always return this to tell VNPay we received the notification
                return Ok(new { RspCode = rspCodeOk, Message = "Confirm Success" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "VNPay IPN unexpected error.");
                return Ok(new { RspCode = rspCodeError, Message = "Unknown error" });
            }
        }
    }
}
