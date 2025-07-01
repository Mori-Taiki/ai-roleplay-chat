namespace AiRoleplayChat.Backend.Services;

public interface IImagenService
{
    /// <summary>
    /// 指定された英語プロンプトに基づいて画像を生成し、そのバイナリデータとMIMEタイプを返します。
    /// </summary>
    /// <param name="prompt">英語のプロンプト</param>
    /// <param name="cancellationToken"></param>
    /// <returns>生成された画像の(バイナリデータ, MIMEタイプ)のタプル。生成に失敗した場合は null。</returns>
    Task<(byte[]? ImageBytes, string? MimeType)?> GenerateImageAsync(string prompt, CancellationToken cancellationToken = default);
}