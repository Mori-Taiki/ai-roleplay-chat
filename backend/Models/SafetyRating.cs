using System.Text.Json.Serialization;

namespace AiRoleplayChat.Backend.Models;

public class SafetyRating
{
    [JsonPropertyName("category")]
    public required string Category { get; set; }

    [JsonPropertyName("probability")]
    public required string Probability { get; set; }
}