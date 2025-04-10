using AiRoleplayChat.Backend.Models;

namespace AiRoleplayChat.Backend.Services;

public interface IImagenService
{
    /// <summary>
    /// 指定された英語プロンプトに基づいて画像を生成します。
    /// </summary>
    /// <param name="prompt">英語のプロンプト</param>
    /// <param name="cancellationToken"></param>
    /// <returns>生成された画像のMIMEタイプとBase64データ</returns>
    Task<ImageGenerationResponse> GenerateImageAsync(string prompt, CancellationToken cancellationToken = default);
}