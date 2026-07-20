using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TechStore.Domain.DTOs.Gemini
{
    public class GeminiRequest
    {
        [JsonPropertyName("systemInstruction")]
        public SystemInstruction? SystemInstruction { get; set; }

        [JsonPropertyName("contents")]
        public List<Content> Contents { get; set; } = new();

        [JsonPropertyName("tools")]
        public List<Tool>? Tools { get; set; }
    }

    public class SystemInstruction
    {
        [JsonPropertyName("parts")]
        public List<Part> Parts { get; set; } = new();
    }

    public class Content
    {
        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("parts")]
        public List<Part> Parts { get; set; } = new();
    }

    public class Part
    {
        [JsonPropertyName("text")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Text { get; set; }

        [JsonPropertyName("functionCall")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public FunctionCall? FunctionCall { get; set; }

        [JsonPropertyName("functionResponse")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public FunctionResponse? FunctionResponse { get; set; }
    }

    public class FunctionCall
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("args")]
        public Dictionary<string, object>? Args { get; set; }
    }

    public class FunctionResponse
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("response")]
        public object Response { get; set; }
    }

    public class Tool
    {
        [JsonPropertyName("functionDeclarations")]
        public List<FunctionDeclaration> FunctionDeclarations { get; set; } = new();
    }

    public class FunctionDeclaration
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("parameters")]
        public ParametersSchema Parameters { get; set; }
    }

    public class ParametersSchema
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "OBJECT";

        [JsonPropertyName("properties")]
        public Dictionary<string, PropertySchema> Properties { get; set; } = new();

        [JsonPropertyName("required")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string>? Required { get; set; }
    }

    public class PropertySchema
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }
    }

    public class GeminiResponse
    {
        [JsonPropertyName("candidates")]
        public List<Candidate> Candidates { get; set; } = new();
    }

    public class Candidate
    {
        [JsonPropertyName("content")]
        public Content Content { get; set; }

        [JsonPropertyName("finishReason")]
        public string? FinishReason { get; set; }
    }
}
