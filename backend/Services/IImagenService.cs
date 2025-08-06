namespace AiRoleplayChat.Backend.Services;

public interface IImagenService
{
    /// <summary>
    /// 指定された英語プロンプトに基づいて画像を生成し、そのバイナリデータとMIMEタイプを返します。
    /// </summary>
    /// <param name="prompt">英語のプロンプト</param>
    /// <param name="userId">ユーザーID（BYOKでユーザー専用APIキーを使用する場合）</param>
    /// <param name="cancellationToken"></param>
    /// <returns>生成された画像の(バイナリデータ, MIMEタイプ)のタプル。生成に失敗した場合は null。</returns>
    Task<(byte[]? ImageBytes, string? MimeType)?> GenerateImageAsync(string prompt, int? userId = null, CancellationToken cancellationToken = default);
}