// backend/Models/CreateCharacterProfileRequest.cs
using System.ComponentModel.DataAnnotations;

namespace AiRoleplayChat.Backend.Models;

public record CreateCharacterProfileRequest(
    [Required(ErrorMessage = "キャラクター名は必須です。")]
    [StringLength(30, ErrorMessage = "キャラクター名は30文字以内で入力してください。")]
    string Name,

    string? Personality,
    string? Tone,
    string? Backstory,

    string? SystemPrompt,
    string? ExampleDialogue,

    [Url(ErrorMessage = "有効なURLを入力してください。")] // URL形式のバリデーション
    string? AvatarImageUrl,

    [StringLength(2000, ErrorMessage = "容姿は2000文字以内で入力してください。")]
    string? Appearance,

    [StringLength(30, ErrorMessage = "ユーザーの呼び方は30文字以内で入力してください。")]
    string? UserAppellation,

    /// <summary>
    /// AI generation settings for this character. If null, uses user defaults or system defaults.
    /// </summary>
    AiGenerationSettingsRequest? AiSettings
);