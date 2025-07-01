using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace AiRoleplayChat.Backend.Services;

public class BlobStorageService : IBlobStorageService
{
    private readonly BlobContainerClient _containerClient;
    private readonly ILogger<BlobStorageService> _logger;

    public BlobStorageService(IConfiguration configuration, ILogger<BlobStorageService> logger)
    {
        _logger = logger;
        var connectionString = configuration["BlobStorage:ConnectionString"] ?? throw new InvalidOperationException("Configuration missing: BlobStorage:ConnectionString");
        var containerName = configuration["BlobStorage:ContainerName"] ?? throw new InvalidOperationException("Configuration missing: BlobStorage:ContainerName");

        // BlobServiceClient を作成し、コンテナクライアントを取得
        var blobServiceClient = new BlobServiceClient(connectionString);
        _containerClient = blobServiceClient.GetBlobContainerClient(containerName);

        // コンテナが存在しない場合は作成する（初回起動時など）
        _containerClient.CreateIfNotExists(PublicAccessType.Blob);
        _logger.LogInformation("BlobStorageService initialized for container: {ContainerName}", containerName);
    }

    public async Task<string> UploadAsync(Stream content, string blobName, string contentType, CancellationToken cancellationToken = default)
    {
        if (content == null || content.Length == 0)
        {
            throw new ArgumentException("Content stream cannot be null or empty.", nameof(content));
        }

        // ストリームの位置を先頭に戻す（重要）
        content.Position = 0;

        try
        {
            var blobClient = _containerClient.GetBlobClient(blobName);

            var blobHttpHeader = new BlobHttpHeaders { ContentType = contentType };

            // Blobをアップロード（上書きを許可）
            await blobClient.UploadAsync(content, new BlobUploadOptions { HttpHeaders = blobHttpHeader }, cancellationToken);

            _logger.LogInformation("Successfully uploaded {BlobName} to container {ContainerName}.", blobName, _containerClient.Name);

            // アップロードされたBlobのURLを返す
            return blobClient.Uri.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading blob {BlobName}.", blobName);
            throw; // エラーを再スローして呼び出し元で処理
        }
    }
}