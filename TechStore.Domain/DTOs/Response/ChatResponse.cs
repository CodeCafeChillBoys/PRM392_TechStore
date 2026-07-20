using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TechStore.Domain.DTOs.Response
{
    public class ChatResponse
    {
        [JsonPropertyName("reply")]
        public string Reply { get; set; } = string.Empty;
    }
}
