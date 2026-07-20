using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechStore.Domain.DTOs.Request;
using TechStore.Domain.DTOs.Response;
using TechStore.Service.IService;

namespace TechStoreAPI.Controllers
{
    /// <summary>
    /// Quan ly nguoi dung — CHI Admin. Thay cho PUT /api/auth/set-role (an toan hon).
    /// </summary>
    [Route("api/users")]
    [ApiController]
    [Authorize(Policy = "AdminOnly")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IMapper _mapper;

        public UsersController(IUserService userService, IMapper mapper)
        {
            _userService = userService;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserSummaryDTO>>> GetUsers([FromQuery] string? role)
        {
            var users = await _userService.GetUsersAsync(role);
            return Ok(_mapper.Map<IEnumerable<UserSummaryDTO>>(users));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<UserSummaryDTO>> GetUser(Guid id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null) return NotFound("Không tìm thấy người dùng.");
            return Ok(_mapper.Map<UserSummaryDTO>(user));
        }

        [HttpPut("{id}/role")]
        public async Task<ActionResult<UserSummaryDTO>> ChangeRole(Guid id, [FromBody] ChangeRoleRequest request)
        {
            var (user, error) = await _userService.ChangeRoleAsync(id, request.Role);
            if (user == null) return BadRequest(error);
            return Ok(_mapper.Map<UserSummaryDTO>(user));
        }
    }
}
