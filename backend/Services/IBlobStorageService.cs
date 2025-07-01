namespace AiRoleplayChat.Backend.Services;

public interface IBlobStorageService
{
    /// <summary>
    /// 指定されたストリームのコンテンツをBlobにアップロードし、そのURLを返します。
    /// </summary>
    /// <param name="content">アップロードするファイルのストリーム。</param>
    /// <param name="blobName">Blobの名前 (ファイル名)。一意になるように設定します。</param>
    /// <param name="contentType">コンテンツのMIMEタイプ (例: "image/png")。</param>
    /// <param name="cancellationToken">CancellationToken。</param>
    /// <returns>アップロードされたBlobの公開URL。</returns>
    Task<string> UploadAsync(Stream content, string blobName, string contentType, CancellationToken cancellationToken = default);
}