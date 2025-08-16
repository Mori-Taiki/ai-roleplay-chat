namespace AiRoleplayChat.Backend.Models;

/// <summary>
/// AI generation settings response model
/// </summary>
public record AiGenerationSettingsResponse(
    int Id,
    string SettingsType,
    string? ChatGenerationProvider,
    string? ChatGenerationModel,
    string? ImagePromptGenerationProvider,
    string? ImagePromptGenerationModel,
    string? ImageGenerationProvider,
    string? ImageGenerationModel,
    string? ImageGenerationPromptInstruction
);