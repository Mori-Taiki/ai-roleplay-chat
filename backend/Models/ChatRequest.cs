using System.ComponentModel.DataAnnotations;

namespace AiRoleplayChat.Backend.Models;

public record ChatRequest(
    [Required(ErrorMessage = "プロンプトは必須です。")]
    string Prompt,

    [Required(ErrorMessage = "キャラクターIDは必須です。")]
    [Range(1, int.MaxValue, ErrorMessage = "キャラクターIDは1以上の値を指定してください。")] // IDが1以上であることを保証
    int CharacterProfileId // この行を追加
);