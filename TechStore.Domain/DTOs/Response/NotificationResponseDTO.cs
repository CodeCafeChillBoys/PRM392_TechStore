using System;

namespace TechStore.Domain.DTOs.Response
{
    public class NotificationResponseDTO
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string Icon { get; set; } = "bell";
        public string Tone { get; set; } = "accent";
        public bool Unread { get; set; } // Phục vụ thuộc tính 'unread' trên Flutter
        public string Time { get; set; } = string.Empty; // Format dạng thân thiện (VD: "10 phút trước")
    }
}
