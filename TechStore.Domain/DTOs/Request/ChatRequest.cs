using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using TechStore.Domain.DTOs.Chat;

namespace TechStore.Domain.DTOs.Request
{
    public class ChatRequest
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("history")]
        public List<ChatMessage> History { get; set; } = new();
    }
}
