using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using TechStore.Domain.DTOs.Response;
using TechStore.Domain.Enum;
using TechStore.Domain.Models;
using TechStore.Repository.IRepositories;
using TechStore.Service.IService;

namespace TechStore.Service.Service
{
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IFirebaseNotificationService _firebaseNotificationService;

        public NotificationService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IFirebaseNotificationService firebaseNotificationService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _firebaseNotificationService = firebaseNotificationService;
        }

        public async Task<NotificationFeedsResponseDTO> GetNotificationsAsync(Guid userId)
        {
            // Lấy thông báo cá nhân và hệ thống (UserId = null)
            var list = await _unitOfWork.Notifications
                .FindAsync(n => n.UserId == userId || n.UserId == null);

            var sortedList = list.OrderByDescending(n => n.CreatedAt).ToList();
            var dtos = _mapper.Map<List<NotificationResponseDTO>>(sortedList);

            // Phân nhóm theo Enum Type chuyển đổi từ Entity
            return new NotificationFeedsResponseDTO
            {
                Promo = dtos.Where(n => sortedList.First(s => s.Id == n.Id).Type == NotificationType.Promo).ToList(),
                Orders = dtos.Where(n => sortedList.First(s => s.Id == n.Id).Type == NotificationType.Order).ToList()
            };
        }

        public async Task<bool> MarkAsReadAsync(Guid id)
        {
            var notification = await _unitOfWork.Notifications.GetByIdAsync(id);
            if (notification == null) return false;

            notification.IsRead = true;
            _unitOfWork.Notifications.Update(notification);
            await _unitOfWork.CompleteAsync();
            return true;
        }

        public async Task<bool> MarkAllAsReadAsync(Guid userId)
        {
            var unreadList = await _unitOfWork.Notifications
                .FindAsync(n => n.UserId == userId && !n.IsRead);

            foreach (var item in unreadList)
            {
                item.IsRead = true;
                _unitOfWork.Notifications.Update(item);
            }

            await _unitOfWork.CompleteAsync();
            return true;
        }

        public async Task CreateAndSendNotificationAsync(Guid userId, string title, string body, NotificationType type, NotificationIcon icon, NotificationTone tone)
        {
            // 1. Lưu thông báo vào DB
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Body = body,
                Type = type,
                Icon = icon,
                Tone = tone,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            await _unitOfWork.Notifications.AddAsync(notification);
            await _unitOfWork.CompleteAsync();

            // 2. Tìm các token thiết bị của User để đẩy Push Notification
            var devices = await _unitOfWork.UserDevices.FindAsync(d => d.UserId == userId);

            foreach (var device in devices)
            {
                if (!string.IsNullOrEmpty(device.FcmToken))
                {
                    await _firebaseNotificationService.SendNotificationAsync(device.FcmToken, title, body);
                }
            }
        }

        public async Task BroadcastNotificationAsync(string title, string body, NotificationType type, NotificationIcon icon, NotificationTone tone)
        {
            // 1. Lưu thông báo với UserId = null (cho tất cả người dùng)
            var notification = new Notification
            {
                UserId = null,
                Title = title,
                Body = body,
                Type = type,
                Icon = icon,
                Tone = tone,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            await _unitOfWork.Notifications.AddAsync(notification);
            await _unitOfWork.CompleteAsync();

            // 2. Lấy tất cả token thiết bị của toàn bộ hệ thống
            var devices = await _unitOfWork.UserDevices.GetAllAsync();

            foreach (var device in devices)
            {
                if (!string.IsNullOrEmpty(device.FcmToken))
                {
                    await _firebaseNotificationService.SendNotificationAsync(device.FcmToken, title, body);
                }
            }
        }
    }
}
