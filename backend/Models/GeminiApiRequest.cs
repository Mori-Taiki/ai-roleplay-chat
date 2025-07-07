using System.Text.Json.Serialization;

namespace AiRoleplayChat.Backend.Models;

public record GeminiApiRequest
{
    [JsonPropertyName("contents")]
    public required GeminiContent[] Contents { get; set; }

    [JsonPropertyName("generationConfig")]
    public required GeminiGenerationConfig GenerationConfig { get; set; }

    [JsonPropertyName("system_instruction")]
    public GeminiContent? SystemInstruction { get; set; }
    
    [JsonPropertyName("safetySettings")]
    public GeminiSafetySetting[]? SafetySettings { get; set; }
}