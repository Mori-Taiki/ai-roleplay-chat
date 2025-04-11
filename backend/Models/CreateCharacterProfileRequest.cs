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

    // 例: ExampleDialogue は JSON 文字列として受け取る想定
    string? ExampleDialogue, // 必要であればバリデーション追加 (JSON形式かなど)

    [Url(ErrorMessage = "有効なURLを入力してください。")] // URL形式のバリデーション
    string? AvatarImageUrl
);