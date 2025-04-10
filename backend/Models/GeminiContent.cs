using System.Text.Json.Serialization;

namespace AiRoleplayChat.Backend.Models;

public class GeminiContent
{
    [JsonPropertyName("parts")]
    public required GeminiPart[] Parts { get; set; }
}