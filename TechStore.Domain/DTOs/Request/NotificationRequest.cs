using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TechStore.Domain.Enum;

namespace TechStore.Domain.DTOs.Request
{
    public class NotificationRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public NotificationType Type { get; set; }
        public NotificationIcon Icon { get; set; }
        public NotificationTone Tone { get; set; }
    }
}