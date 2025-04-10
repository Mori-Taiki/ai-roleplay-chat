using System.Text.Json.Serialization;

namespace AiRoleplayChat.Backend.Models;

public class GeminiApiRequest
{
    [JsonPropertyName("contents")]
    public required GeminiContent[] Contents { get; set; }
    
    [JsonPropertyName("generationConfig")] 
    public required GeminiGenerationConfig GenerationConfig { get; set; }
}