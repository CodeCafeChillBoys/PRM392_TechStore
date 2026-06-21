using System;
using System.Linq;
using System.Threading.Tasks;
using TechStore.Domain.DTOs.Request;
using TechStore.Domain.Models;
using TechStore.Repository.IRepositories;
using TechStore.Service.IService;

namespace TechStore.Service.Service
{
    public class DeviceService : IDeviceService
    {
        private readonly IUnitOfWork _unitOfWork;

        public DeviceService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task RegisterDeviceTokenAsync(Guid userId, RegisterDeviceRequest request)
        {
            var existingDevices = await _unitOfWork.UserDevices.FindAsync(d => d.FcmToken == request.FcmToken);
            var existingDevice = existingDevices.FirstOrDefault();

            if (existingDevice != null)
            {
                existingDevice.UserId = userId;
                existingDevice.DeviceType = request.DeviceType;
                existingDevice.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.UserDevices.Update(existingDevice);
            }
            else
            {
                var newDevice = new UserDevice
                {
                    UserId = userId,
                    FcmToken = request.FcmToken,
                    DeviceType = request.DeviceType,
                    UpdatedAt = DateTime.UtcNow
                };
                await _unitOfWork.UserDevices.AddAsync(newDevice);
            }

            await _unitOfWork.CompleteAsync();
        }
    }
}
