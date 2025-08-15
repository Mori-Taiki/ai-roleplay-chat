namespace AiRoleplayChat.Backend.Models;

public record CharacterProfileResponse(
    int Id,
    string Name,
    string? Personality,
    string? Tone,
    string? Backstory,
    string? SystemPrompt,
    string? ExampleDialogue,
    string? AvatarImageUrl,
    bool IsActive,
    bool IsSystemPromptCustomized,
    string? Appearance,
    string? UserAppellation,
    string? TextModelProvider,
    string? TextModelId,
    string? ImageModelProvider,
    string? ImageModelId
);