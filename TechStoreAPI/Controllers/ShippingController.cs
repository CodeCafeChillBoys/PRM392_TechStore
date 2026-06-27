using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TechStore.Domain.Constants;
using TechStore.Domain.DTOs.Request;
using TechStore.Domain.DTOs.Response;
using TechStore.Service.IService;

namespace TechStoreAPI.Controllers
{
    [ApiController]
    [Route("api/shipping")]
    public class ShippingController : ControllerBase
    {
        private readonly IShippingService _shippingService;

        public ShippingController(IShippingService shippingService)
        {
            _shippingService = shippingService;
        }

        [HttpPost("calculate")]
        public async Task<ActionResult<ShippingCalculationResponse>> CalculateShipping([FromBody] ShippingCalculationRequest request)
        {
            if (request == null)
            {
                return BadRequest(ShippingConstants.InvalidRequest);
            }

            if (request.DestinationLat == 0 || request.DestinationLng == 0)
            {
                return BadRequest(ShippingConstants.InvalidDestinationCoordinates);
            }

            try
            {
                var response = await _shippingService.CalculateShippingAsync(request);
                return Ok(response);
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, string.Format(ShippingConstants.CalculateShippingFailed, ex.Message));
            }
        }
    }
}
