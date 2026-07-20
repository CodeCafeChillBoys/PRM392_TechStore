using Microsoft.AspNetCore.Mvc;
using TechStore.Domain.Enum;
using TechStore.Service.IService;

namespace TechStoreAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly IVnpayService _vnpayService;
        private readonly IWalletService _walletService;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(
            IVnpayService vnpayService,
            IWalletService walletService,
            ILogger<PaymentController> logger)
        {
            _vnpayService = vnpayService;
            _walletService = walletService;
            _logger = logger;
        }

        /// <summary>
        /// VNPay redirects the user's browser here after a wallet top-up.
        /// The IPN endpoint remains the authoritative server-to-server callback.
        /// </summary>
        [HttpGet("vnpay-return")]
        public async Task<IActionResult> VnpayReturn()
        {
            var isValidSignature = _vnpayService.ValidateSignature(
                Request.QueryString.Value ?? string.Empty,
                out var responseCode,
                out var vnpayTransactionId,
                out var transactionReference);

            if (!isValidSignature)
            {
                _logger.LogWarning("VNPay Return: invalid signature. Query: {Query}", Request.QueryString);
                return BadRequest(new { success = false, message = "Chữ ký VNPay không hợp lệ." });
            }

            if (!Guid.TryParse(transactionReference, out var transactionId))
                return BadRequest(new { success = false, message = "Mã giao dịch nạp ví không hợp lệ." });

            if (!TryGetPaidAmount(out var paidAmount))
                return BadRequest(new { success = false, message = "Số tiền VNPay trả về không hợp lệ." });

            var isPaid = IsSuccessfulVnpayPayment(responseCode);
            var result = await _walletService.ConfirmTopUpAsync(
                transactionId,
                isPaid,
                vnpayTransactionId,
                paidAmount);

            var topUpSucceeded = isPaid && result is
                TopUpConfirmationStatus.Completed or TopUpConfirmationStatus.AlreadyProcessed;

            _logger.LogInformation(
                "VNPay wallet return processed — TransactionId: {TransactionId}, VnpayTransactionId: {VnpayTransactionId}, Result: {Result}",
                transactionId,
                vnpayTransactionId,
                result);

            var deepLink =
                $"techstore://wallet-topup-result?success={topUpSucceeded.ToString().ToLowerInvariant()}" +
                $"&transactionId={transactionId}&amount={paidAmount:0}&paymentMethod=VNPay";
            return Redirect(deepLink);
        }

        /// <summary>VNPay's authoritative server-to-server callback for wallet top-ups.</summary>
        [HttpGet("vnpay-ipn")]
        public async Task<IActionResult> VnpayIpn()
        {
            const string responseOk = "00";
            const string responseNotFound = "01";
            const string responseInvalidAmount = "04";
            const string responseInvalidSignature = "97";
            const string responseError = "99";

            try
            {
                var isValidSignature = _vnpayService.ValidateSignature(
                    Request.QueryString.Value ?? string.Empty,
                    out var responseCode,
                    out var vnpayTransactionId,
                    out var transactionReference);

                if (!isValidSignature)
                {
                    _logger.LogWarning("VNPay IPN: invalid signature from IP {IP}", HttpContext.Connection.RemoteIpAddress);
                    return Ok(new { RspCode = responseInvalidSignature, Message = "Invalid Checksum" });
                }

                if (!Guid.TryParse(transactionReference, out var transactionId))
                    return Ok(new { RspCode = responseNotFound, Message = "Transaction not found" });

                if (!TryGetPaidAmount(out var paidAmount))
                    return Ok(new { RspCode = responseInvalidAmount, Message = "Invalid amount" });

                var result = await _walletService.ConfirmTopUpAsync(
                    transactionId,
                    IsSuccessfulVnpayPayment(responseCode),
                    vnpayTransactionId,
                    paidAmount);

                _logger.LogInformation(
                    "VNPay wallet IPN processed — TransactionId: {TransactionId}, VnpayTransactionId: {VnpayTransactionId}, Result: {Result}",
                    transactionId,
                    vnpayTransactionId,
                    result);

                return result switch
                {
                    TopUpConfirmationStatus.NotFound =>
                        Ok(new { RspCode = responseNotFound, Message = "Transaction not found" }),
                    TopUpConfirmationStatus.AmountMismatch =>
                        Ok(new { RspCode = responseInvalidAmount, Message = "Invalid amount" }),
                    _ => Ok(new { RspCode = responseOk, Message = "Confirm Success" })
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "VNPay wallet IPN unexpected error.");
                return Ok(new { RspCode = responseError, Message = "Unknown error" });
            }
        }

        private bool TryGetPaidAmount(out decimal amount)
        {
            amount = 0;
            return long.TryParse(Request.Query["vnp_Amount"].ToString(), out var rawAmount) &&
                   rawAmount > 0 &&
                   (amount = rawAmount / 100m) > 0;
        }

        private bool IsSuccessfulVnpayPayment(string responseCode)
        {
            var transactionStatus = Request.Query["vnp_TransactionStatus"].ToString();
            return responseCode == "00" &&
                   (string.IsNullOrEmpty(transactionStatus) || transactionStatus == "00");
        }
    }
}
