using System.Text.Json.Serialization;

namespace AiRoleplayChat.Backend.Models;

public class GeminiCandidate
{
    [JsonPropertyName("content")]
    public required GeminiContent Content { get; set; }

    [JsonPropertyName("finishReason")]
    public required string FinishReason { get; set; }

    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("safetyRatings")]
    public SafetyRating[]? SafetyRatings { get; set; }
}