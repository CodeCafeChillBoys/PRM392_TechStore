using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using TechStore.Service.DTO.Request;
using TechStore.Service.DTO.Respone;
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
        public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] TechStore.Service.DTO.Request.LoginRequest request)
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
    }
}