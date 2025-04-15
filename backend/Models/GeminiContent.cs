using System.Text.Json.Serialization;

namespace AiRoleplayChat.Backend.Models;

public class GeminiContent
{
    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("parts")]
    public required GeminiPart[] Parts { get; set; }
}