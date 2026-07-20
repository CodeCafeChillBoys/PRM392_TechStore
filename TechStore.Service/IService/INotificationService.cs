using System;
using System.Threading.Tasks;
using TechStore.Domain.DTOs.Request;
using TechStore.Domain.DTOs.Response;
using TechStore.Domain.Enum;

namespace TechStore.Service.IService
{
    public interface INotificationService
    {
        Task<NotificationFeedsResponseDTO> GetNotificationsAsync(Guid userId);
        Task<bool> MarkAsReadAsync(Guid id);
        Task<bool> MarkAllAsReadAsync(Guid userId);
        Task CreateAndSendNotificationAsync(CreateNotificationRequest request);

        Task BroadcastNotificationAsync(NotificationRequest request);
    }
}
