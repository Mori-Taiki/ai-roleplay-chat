using System.Text.Json.Serialization;

namespace AiRoleplayChat.Backend.Models;

public class GeminiPart
{
    [JsonPropertyName("text")]
    public required string Text { get; set; }
}