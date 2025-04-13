using System.ComponentModel.DataAnnotations;

namespace AiRoleplayChat.Backend.Models; // Use actual namespace

public record UpdateCharacterProfileRequest(
    [Required(ErrorMessage = "キャラクター名は必須です。")]
    [StringLength(30, ErrorMessage = "キャラクター名は30文字以内で入力してください。")]
    string Name,

    string? Personality,
    string? Tone,
    string? Backstory,
    string? SystemPrompt,
    string? ExampleDialogue,
    [Url(ErrorMessage = "有効なURLを入力してください。")]
    string? AvatarImageUrl,
    bool IsActive,
    bool IsSystemPromptCustomized
);