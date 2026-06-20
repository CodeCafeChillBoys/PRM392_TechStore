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
        [HttpPost("login")]
        public async Task<ActionResult<ApiResponse<TwoFactorLoginResponse>>> Login([FromBody] LoginRequest request)
        {
            var result = await _authService.LoginAsync(request);
            if (result == null || !result.success)
                return BadRequest(result);
            return Ok(result);
        }

        [HttpPost("refresh-token")]
        public async Task<ActionResult<ApiResponse<LoginResponse>>> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            var result = await _authService.RefreshTokenAsync(request);

            if (result == null || !result.success)
                return BadRequest(result);

            return Ok(result);
        }

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

        [HttpGet("verify-link")]
        public async Task<IActionResult> VerifyLink([FromQuery] string token)
        {
            var result = await _authService.VerifyEmailLinkAsync(token);
            if (result == null || !result.success)
            {
                return Content("<h3>Xác thực liên kết không thành công hoặc liên kết đã hết hạn.</h3>", "text/html; charset=utf-8");
            }

            // Redirect to Flutter deep link (Custom Scheme)
            var flutterDeepLink = $"techstore://otp-verify?token={token}";
            return Redirect(flutterDeepLink);
        }

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