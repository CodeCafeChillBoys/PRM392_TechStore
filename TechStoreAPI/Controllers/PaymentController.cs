using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TechStore.Service.IService;

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
        // Returns a JSON response containing order status details.
        // NOTE: DB update happens here as fallback; IPN is the primary source.
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("vnpay-return")]
        public async Task<IActionResult> VnpayReturn()
        {
            var isValidSignature = _vnpayService.ValidateSignature(
                Request.QueryString.Value ?? string.Empty,
                out var responseCode,
                out var transactionId,
                out var orderIdStr);

            if (!isValidSignature)
            {
                _logger.LogWarning("VNPay Return: invalid signature. Query: {Query}", Request.QueryString);
                return BadRequest(new { success = false, message = "Chữ ký không hợp lệ. Giao dịch có thể bị giả mạo." });
            }

            bool isPaid = responseCode == "00";

            // Parse amount from query (VNPay sends amount * 100)
            var rawAmount = Request.Query["vnp_Amount"].ToString();
            var amountDisplay = long.TryParse(rawAmount, out var amtRaw)
                ? $"{amtRaw / 100:N0} VND"
                : rawAmount;

            _logger.LogInformation(
                "VNPay Return — OrderId: {OrderId}, ResponseCode: {Code}, TxnId: {TxnId}, Paid: {IsPaid}",
                orderIdStr, responseCode, transactionId, isPaid);

            if (!Guid.TryParse(orderIdStr, out var orderId))
            {
                return BadRequest(new { success = false, message = "Mã đơn hàng không hợp lệ." });
            }

            // Update DB (fallback — IPN is primary)
            await _orderService.ConfirmVnpayPaymentAsync(orderId, isPaid, transactionId);

            double amount = 0;
            if (long.TryParse(Request.Query["vnp_Amount"].ToString(), out var returnAmtRaw))
            {
                amount = (double)returnAmtRaw / 100;
            }

            var flutterDeepLink = $"techstore://payment-result?success={isPaid.ToString().ToLower()}&orderId={orderIdStr}&amount={amount}&paymentMethod=VNPay";
            return Redirect(flutterDeepLink);
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
            const string rspCodeOk       = "00";
            const string rspCodeInvalid  = "97";
            const string rspCodeNotFound = "01";
            const string rspCodeError    = "99";

            try
            {
                var isValidSignature = _vnpayService.ValidateSignature(
                    Request.QueryString.Value ?? string.Empty,
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
