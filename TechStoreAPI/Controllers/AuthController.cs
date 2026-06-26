using Microsoft.AspNetCore.Mvc;
using TechStore.Domain.DTOs.Request;
using TechStore.Domain.DTOs.Response;
using TechStore.Service.IService;

namespace TechStoreAPI.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }
        /// <summary>
        /// Đăng nhập bằng google.
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <param name="request">Thông tin đăng nhập gồm email và password</param>
        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
        {
            var result = await _authService.GoogleLoginAsync(request);
            if (result == null || !result.success)
                return BadRequest(result);
            return Ok(result);
        }
        
        /// <summary>
        /// Đăng nhập tài khoản bằng Email và Mật khẩu.
        /// </summary>
        /// <remarks>
        /// Trả về verifyToken ở trạng thái Pending. Sau đó Client cần điều hướng qua màn hình chọn phương thức xác thực.
        /// </remarks>
        /// <param name="request">Thông tin đăng nhập gồm email và password</param>
        [HttpPost("login")]
        public async Task<ActionResult<ApiResponse<TwoFactorLoginResponse>>> Login([FromBody] LoginRequest request)
        {
            var result = await _authService.LoginAsync(request);
            if (result == null || !result.success)
                return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// Làm mới Access Token khi hết hạn bằng Refresh Token.
        /// </summary>
        /// <param name="request">Refresh Token hiện tại</param>
        [HttpPost("refresh-token")]
        public async Task<ActionResult<ApiResponse<LoginResponse>>> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            var result = await _authService.RefreshTokenAsync(request);

            if (result == null || !result.success)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// Đăng ký tài khoản người dùng mới.
        /// </summary>
        /// <param name="request">Thông tin tài khoản đăng ký mới</param>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] CreateUserRequest request)
        {
            var result = await _authService.RegisterAsync(request);
            if (result == null || !result.success)
            {
                return BadRequest(result);
            }

            return StatusCode(201, result);
        }

        /// <summary>
        /// Điểm cuối xử lý khi người dùng nhấp vào link xác nhận (Magic Link) trong Email.
        /// </summary>
        /// <remarks>
        /// Kích hoạt trạng thái session thành Approved và Redirect về Flutter deep link: techstore://login-success
        /// </remarks>
        /// <param name="token">Mã verifyToken của phiên đăng nhập</param>
        [HttpGet("verify-email-link")]
        public async Task<IActionResult> VerifyEmailLinkAsync([FromQuery] string token)
        {
            var result = await _authService.VerifyEmailLinkAsync(token);
            if (result == null || !result.success)
            {
                return Content(TechStore.Domain.Constants.HtmlTemplates.GetVerifyDeviceFailedPage(), "text/html; charset=utf-8");
            }

            var flutterDeepLink = $"techstore://login-success?accessToken={result.Data!.AccessToken}&refreshToken={result.Data.RefreshToken}&expiresIn={result.Data.ExpiresIn}";
            return Redirect(flutterDeepLink);
        }

        /// <summary>
        /// Kích hoạt gửi mã OTP và chuyển tiếp trình duyệt về app (hỗ trợ luồng email cũ).
        /// </summary>
        /// <remarks>
        /// Gửi OTP qua email và redirect trình duyệt về Flutter deep link: techstore://otp-verify
        /// </remarks>
        /// <param name="token">Mã verifyToken của phiên đăng nhập</param>
        [HttpGet("send-otp")]
        public async Task<IActionResult> SendOtp([FromQuery] string token)
        {
            var result = await _authService.SendOtpTriggerAsync(token);
            if (result == null || !result.success)
            {
                return Content(TechStore.Domain.Constants.HtmlTemplates.GetSendOtpFailedPage(), "text/html; charset=utf-8");
            }

            var flutterDeepLink = $"techstore://otp-verify?token={token}";
            return Redirect(flutterDeepLink);
        }

        /// <summary>
        /// Kiểm tra trạng thái hiện tại của phiên đăng nhập (Pending hoặc Approved).
        /// </summary>
        /// <remarks>
        /// Dùng cho Client gọi Polling liên tục kiểm tra xem User đã click link xác nhận email trên thiết bị khác chưa.
        /// </remarks>
        /// <param name="token">Mã verifyToken của phiên đăng nhập</param>
        [HttpGet("session-status")]
        public async Task<ActionResult<ApiResponse<LoginResponse>>> GetSessionStatus([FromQuery] string token)
        {
            var result = await _authService.GetSessionStatusAsync(token);
            if (result == null || !result.success)
                return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// Gửi email chứa duy nhất liên kết xác thực đăng nhập (Magic Link).
        /// </summary>
        /// <remarks>
        /// Được gọi từ Flutter app khi người dùng chọn phương thức "Xác thực qua Link Email".
        /// </remarks>
        /// <param name="token">Mã verifyToken của phiên đăng nhập</param>
        [HttpGet("send-verify-link")]
        public async Task<IActionResult> SendVerifyLink([FromQuery] string token)
        {
            var result = await _authService.SendVerifyEmailLinkAsync(token);
            if (result == null || !result.success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        /// <summary>
        /// Xác thực mã OTP 6 số do người dùng nhập từ ứng dụng Flutter.
        /// </summary>
        /// <remarks>
        /// Trả về Access Token và Refresh Token nếu mã OTP chính xác để đăng nhập thành công.
        /// </remarks>
        /// <param name="request">Mã xác thực OTP và verifyToken</param>
        [HttpPost("verify-otp")]
        public async Task<ActionResult<ApiResponse<LoginResponse>>> VerifyOtp([FromBody] VerifyOtpRequest request)
        {
            var result = await _authService.VerifyOtpAsync(request);
            if (result == null || !result.success)
                return BadRequest(result);
            return Ok(result);
        }
    }
}