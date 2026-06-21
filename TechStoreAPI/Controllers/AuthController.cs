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

        [HttpGet("verify-device")]
        public async Task<IActionResult> VerifyDevice([FromQuery] string token)
        {
            var result = await _authService.VerifyDeviceAsync(token);
            if (result == null || !result.success)
            {
                return Content(TechStore.Domain.Constants.HtmlTemplates.GetVerifyDeviceFailedPage(), "text/html; charset=utf-8");
            }

            var flutterDeepLink = $"techstore://login-success?accessToken={result.Data!.AccessToken}&refreshToken={result.Data.RefreshToken}&expiresIn={result.Data.ExpiresIn}";
            return Redirect(flutterDeepLink);
        }

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

        [HttpGet("session-status")]
        public async Task<ActionResult<ApiResponse<LoginResponse>>> GetSessionStatus([FromQuery] string token)
        {
            var result = await _authService.GetSessionStatusAsync(token);
            if (result == null || !result.success)
                return BadRequest(result);
            return Ok(result);
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