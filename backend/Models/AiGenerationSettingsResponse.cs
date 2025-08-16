namespace AiRoleplayChat.Backend.Models;

/// <summary>
/// AI generation settings response model
/// </summary>
public record AiGenerationSettingsResponse(
    int Id,
    string? ChatGenerationModel,
    string? ImagePromptGenerationModel,
    string? ImageGenerationModel,
    string? ImageGenerationPromptInstruction
);