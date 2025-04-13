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
    string? AvatarImageUrl
);