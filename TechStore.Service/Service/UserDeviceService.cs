using TechStore.Domain.DTOs.Request;
using TechStore.Domain.Models;
using TechStore.Repository.IRepositories;
using TechStore.Service.IService;

namespace TechStore.Service.Service
{
    public class UserDeviceService : IUserDeviceService
    {
        private readonly IUnitOfWork _unitOfWork;

        public UserDeviceService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<List<UserDevice>> GetOtherDevicesAsync(Guid userId, string currentDeviceId)
        {
            return (await _unitOfWork.UserDevices
                           .FindAsync(x =>
                               x.UserId == userId &&
                               x.DeviceId != currentDeviceId))
                           .ToList();
        }

        public async Task<List<UserDevice>> GetUserDevicesAsync(Guid userId)
        {
            return (await _unitOfWork.UserDevices
                .FindAsync(x => x.UserId == userId))
                .ToList();
        }

        public async Task<bool> RegisterOrUpdateDeviceAsync(Guid userId, RegisterDeviceRequest request)
        {
            var device = (await _unitOfWork.UserDevices
                 .FindAsync(x =>
                     x.UserId == userId &&
                     x.DeviceId == request.DeviceId))
                 .FirstOrDefault();

            if (device == null)
            {
                var newDevice = new UserDevice
                {
                    UserId = userId,
                    DeviceId = request.DeviceId,
                    DeviceName = request.DeviceName,
                    DeviceType = request.DeviceType,
                    FcmToken = request.FcmToken,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _unitOfWork.UserDevices.AddAsync(newDevice);
                await _unitOfWork.CompleteAsync();

                return true; // thiết bị mới
            }

            device.FcmToken = request.FcmToken;
            device.DeviceName = request.DeviceName;
            device.DeviceType = request.DeviceType;
            device.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.UserDevices.Update(device);
            await _unitOfWork.CompleteAsync();

            return false; // thiết bị đã tồn tại
        }
    }
}