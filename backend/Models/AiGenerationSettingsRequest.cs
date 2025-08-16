using System.ComponentModel.DataAnnotations;

namespace AiRoleplayChat.Backend.Models;

/// <summary>
/// AI generation settings that can be included in character profile requests
/// </summary>
public record AiGenerationSettingsRequest(
    [StringLength(50, ErrorMessage = "チャット生成プロバイダーは50文字以内で入力してください。")]
    string? ChatGenerationProvider,

    [StringLength(200, ErrorMessage = "チャット生成モデルは200文字以内で入力してください。")]
    string? ChatGenerationModel,

    [StringLength(50, ErrorMessage = "画像プロンプト生成プロバイダーは50文字以内で入力してください。")]
    string? ImagePromptGenerationProvider,

    [StringLength(200, ErrorMessage = "画像プロンプト生成モデルは200文字以内で入力してください。")]
    string? ImagePromptGenerationModel,

    [StringLength(50, ErrorMessage = "画像生成プロバイダーは50文字以内で入力してください。")]
    string? ImageGenerationProvider,

    [StringLength(200, ErrorMessage = "画像生成モデルは200文字以内で入力してください。")]
    string? ImageGenerationModel,

    [StringLength(2000, ErrorMessage = "画像生成プロンプト指示は2000文字以内で入力してください。")]
    string? ImageGenerationPromptInstruction
);