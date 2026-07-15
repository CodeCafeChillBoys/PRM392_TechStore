using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechStore.Domain.DTOs.Request;
using TechStore.Service.IService;

namespace TechStoreAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/wallet")]
    public class WalletController : ControllerBase
    {
        private readonly IWalletService _walletService;
        private readonly ILogger<WalletController> _logger;

        public WalletController(IWalletService walletService, ILogger<WalletController> logger)
        {
            _walletService = walletService;
            _logger = logger;
        }

        /// <summary>Gets the authenticated user's wallet.</summary>
        [HttpGet]
        public async Task<IActionResult> GetWallet()
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new { message = "Không xác thực được người dùng." });

            try
            {
                return Ok(new { success = true, data = await _walletService.GetWalletAsync(userId) });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>Gets wallet transaction history, newest first.</summary>
        [HttpGet("transactions")]
        public async Task<IActionResult> GetTransactions([FromQuery] int skip = 0, [FromQuery] int take = 50)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new { message = "Không xác thực được người dùng." });

            skip = Math.Max(0, skip);
            take = Math.Clamp(take, 1, 100);

            try
            {
                var transactions = await _walletService.GetTransactionsAsync(userId, skip, take);
                return Ok(new { success = true, data = transactions });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>Creates a pending top-up and returns the VNPay payment URL.</summary>
        [HttpPost("top-up")]
        public async Task<IActionResult> TopUp([FromBody] WalletTopUpRequest request)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new { message = "Không xác thực được người dùng." });

            try
            {
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
                var result = await _walletService.CreateTopUpAsync(userId, request.Amount, ipAddress);
                return Ok(new
                {
                    success = true,
                    message = "Đã tạo giao dịch nạp ví. Vui lòng hoàn tất thanh toán qua VNPay.",
                    data = result
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Cannot create wallet top-up for user {UserId}: {Message}", userId, ex.Message);
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Creates a withdrawal request and reserves the amount immediately.
        /// The minimum withdrawal amount is 50,000 VND and the wallet balance must be greater than 50,000 VND.
        /// </summary>
        [HttpPost("withdraw")]
        public async Task<IActionResult> Withdraw([FromBody] WalletWithdrawalRequest request)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new { message = "Không xác thực được người dùng." });

            try
            {
                var transaction = await _walletService.WithdrawAsync(userId, request);
                return Ok(new
                {
                    success = true,
                    message = "Yêu cầu rút tiền đã được tạo và đang chờ xử lý.",
                    data = transaction
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Cannot withdraw wallet balance for user {UserId}: {Message}", userId, ex.Message);
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        private bool TryGetUserId(out Guid userId)
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(claim, out userId);
        }
    }
}
