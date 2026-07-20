using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechStore.Domain.DTOs.Request;
using TechStore.Domain.DTOs.Response;
using TechStore.Service.IService;

namespace TechStoreAPI.Controllers
{
    /// <summary>
    /// Dashboard van hanh — CHI Admin: thong ke, phien dang nhap, thiet bi, ton kho.
    /// </summary>
    [Route("api/admin")]
    [ApiController]
    [Authorize(Policy = "AdminOnly")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;
        private readonly IMapper _mapper;

        public AdminController(IAdminService adminService, IMapper mapper)
        {
            _adminService = adminService;
            _mapper = mapper;
        }

        [HttpGet("stats")]
        public async Task<ActionResult<AdminStatsResponse>> GetStats(
            [FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var toDate = (to ?? DateTime.UtcNow).ToUniversalTime();
            var fromDate = (from ?? toDate.AddDays(-30)).ToUniversalTime();
            return Ok(await _adminService.GetStatsAsync(fromDate, toDate));
        }

        [HttpGet("sessions")]
        public async Task<ActionResult<IEnumerable<LoginSessionInfoDTO>>> GetSessions()
        {
            var sessions = await _adminService.GetSessionsAsync();
            return Ok(_mapper.Map<IEnumerable<LoginSessionInfoDTO>>(sessions));
        }

        [HttpGet("devices")]
        public async Task<ActionResult<IEnumerable<DeviceInfoDTO>>> GetDevices()
        {
            var devices = await _adminService.GetDevicesAsync();
            return Ok(_mapper.Map<IEnumerable<DeviceInfoDTO>>(devices));
        }

        [HttpGet("products/low-stock")]
        public async Task<ActionResult<IEnumerable<ProductResponseDTO>>> GetLowStock(
            [FromQuery] int threshold = 10)
        {
            var products = await _adminService.GetLowStockAsync(threshold);
            return Ok(_mapper.Map<IEnumerable<ProductResponseDTO>>(products));
        }

        // PUT (khong PATCH) vi ApiClient phia FE khong co patch().
        [HttpPut("products/{id}/stock")]
        public async Task<ActionResult<ProductResponseDTO>> UpdateStock(
            Guid id, [FromBody] UpdateStockRequest request)
        {
            var product = await _adminService.UpdateProductStockAsync(id, request.StockQuantity);
            if (product == null) return NotFound("Không tìm thấy sản phẩm.");
            return Ok(_mapper.Map<ProductResponseDTO>(product));
        }
    }
}
