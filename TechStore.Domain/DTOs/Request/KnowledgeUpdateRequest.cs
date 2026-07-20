using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TechStore.Domain.DTOs.Request
{
    public class KnowledgeUpdateRequest
    {
        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }
}
