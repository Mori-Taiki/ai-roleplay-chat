using System.Text.Json.Serialization;

namespace AiRoleplayChat.Backend.Models;

public class GeminiApiResponse
{
    [JsonPropertyName("candidates")]
    public required GeminiCandidate[] Candidates { get; set; }
}