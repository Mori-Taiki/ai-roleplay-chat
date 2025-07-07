using AiRoleplayChat.Backend.Domain.Entities;

namespace AiRoleplayChat.Backend.Services;

public interface IGeminiService
{
    /// <summary>
    /// 指定されたプロンプトに対するチャット応答を生成します。
    /// </summary>
    Task<string> GenerateChatResponseAsync(string prompt, string systemPrompt, List<ChatMessage> history, CancellationToken cancellationToken = default);
    /// <summary>
    /// 会話履歴から画像生成用の英語プロンプトを生成します。
    /// </summary>
    Task<string> GenerateImagePromptAsync(
        CharacterProfile character, 
        List<ChatMessage> history, 
        CancellationToken cancellationToken = default);
}