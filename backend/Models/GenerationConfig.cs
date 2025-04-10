using System.Text.Json.Serialization;

namespace AiRoleplayChat.Backend.Models;

public class GeminiGenerationConfig
{
    [JsonPropertyName("temperature")]
    public double Temperature { get; set; } = 0.7;

    [JsonPropertyName("maxOutputTokens")]
    public int MaxOutputTokens { get; set; } = 1024;
}