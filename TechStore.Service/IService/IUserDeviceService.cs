using TechStore.Domain.DTOs.Request;
using TechStore.Domain.Models;

namespace TechStore.Service.IService
{
    public interface IUserDeviceService
    {
        Task<bool> RegisterOrUpdateDeviceAsync(Guid userId, RegisterDeviceRequest request);

        Task<List<UserDevice>> GetUserDevicesAsync(Guid userId);
        public Task<List<UserDevice>> GetOtherDevicesAsync(
           Guid userId,
           string currentDeviceId);


    }
}