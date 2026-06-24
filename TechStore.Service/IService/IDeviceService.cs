
using TechStore.Domain.DTOs.Request;

namespace TechStore.Service.IService
{
    public interface IDeviceService
    {
        Task RegisterDeviceTokenAsync(Guid userId, RegisterDeviceRequest request);
    }
}