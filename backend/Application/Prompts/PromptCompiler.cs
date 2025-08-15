using System.Text;
using AiRoleplayChat.Backend.Domain.Entities;

namespace AiRoleplayChat.Backend.Application.Prompts;

/// <summary>
/// Service for compiling and managing prompts for AI interactions
/// Migrated from PromptUtils.SystemPromptHelper
/// </summary>
public class PromptCompiler : IPromptCompiler
{
    // 画像生成の指示を含む定数文字列
    private const string ImageGenerationInstruction = "\n---\n【重要】**あなた自身の行動や感情、あなたが見ている情景**を描写する画像を生成することが、会話をよりリアルで魅力的にすると判断した場合**のみ**、応答の最後に `[generate_image]` というタグを追加してください。";

    /// <summary>
    /// Generate default system prompt for a character
    /// </summary>
    public string GenerateDefaultPrompt(string name, string? personality, string? tone, string? backstory, string? appearance = null, string? userAppellation = null)
    {
        // 元のPromptUtils.SystemPromptHelper.GenerateDefaultPromptからロジックを移植
        var sb = new StringBuilder();
        sb.Append("あなたはキャラクター「").Append(name).AppendLine("」です。");
        sb.Append("性格: ").Append(personality ?? "未設定").AppendLine();
        sb.Append("口調: ").Append(tone ?? "未設定").AppendLine();
        sb.Append("背景: ").Append(backstory ?? "未設定").AppendLine();
        sb.Append("容姿: ").Append(appearance ?? "未設定").AppendLine();
        sb.Append("ユーザーの呼び方: ").Append(userAppellation ?? "未設定").AppendLine();
        sb.Append("ユーザーと自然で魅力的な対話を行ってください。");
        return sb.ToString();
    }

    /// <summary>
    /// Append image generation instruction to a base prompt
    /// </summary>
    public string AppendImageInstruction(string basePrompt)
    {
        // 元のPromptUtils.SystemPromptHelper.AppendImageInstructionからロジックを移植
        if (string.IsNullOrWhiteSpace(basePrompt))
        {
            return ImageGenerationInstruction; // ベースが空なら指示だけ返す
        }
        var sb = new StringBuilder(basePrompt);
        sb.Append(ImageGenerationInstruction);
        return sb.ToString();
    }

    /// <summary>
    /// Get the image generation instruction text
    /// </summary>
    public string GetImageGenerationInstruction()
    {
        return ImageGenerationInstruction;
    }
}