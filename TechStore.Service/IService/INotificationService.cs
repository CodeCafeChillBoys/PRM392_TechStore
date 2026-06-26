using System;
using System.Threading.Tasks;
using TechStore.Domain.DTOs.Response;
using TechStore.Domain.Enum;

namespace TechStore.Service.IService
{
    public interface INotificationService
    {
        Task<NotificationFeedsResponseDTO> GetNotificationsAsync(Guid userId);
        Task<bool> MarkAsReadAsync(Guid id);
        Task<bool> MarkAllAsReadAsync(Guid userId);
        Task CreateAndSendNotificationAsync(Guid userId, string title, string body, NotificationType type, NotificationIcon icon, NotificationTone tone);
        Task BroadcastNotificationAsync(string title, string body, NotificationType type, NotificationIcon icon, NotificationTone tone);
    }
}
