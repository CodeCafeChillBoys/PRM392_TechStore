using TechStore.Domain.DTOs.Request;
using TechStore.Domain.DTOs.Response;

namespace TechStore.Service.IService
{
    public interface IShippingService
    {
        Task<ShippingCalculationResponse> CalculateShippingAsync(ShippingCalculationRequest request);
    }
}