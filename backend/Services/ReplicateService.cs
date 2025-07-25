using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization; // ILoggerを追加

namespace AiRoleplayChat.Backend.Services;

public class ReplicateService : IImagenService
{
    // --- APIリクエスト/レスポンス用の内部レコード定義 ---

    // APIに送信するリクエストのinput部分
    private record ReplicateInput(
        [property: JsonPropertyName("prompt")] string Prompt,
        [property: JsonPropertyName("negative_prompt")] string NegativePrompt
    );

    // APIに送信するリクエスト全体
    private record ReplicateRequest([property: JsonPropertyName("version")] string Version, [property: JsonPropertyName("input")] ReplicateInput Input);

    // 最初にPOSTしたときに返ってくるレスポンス
    private record InitialResponse(
        [property: JsonPropertyName("urls")] Urls? Urls
    );
    private record Urls([property: JsonPropertyName("get")] string Get);

    // ポーリング時に返ってくるレスポンス
    private record PollingResponse(
        [property: JsonPropertyName("output")] JsonElement? Output, // URLは配列 or 文字列の場合があるためJsonElementで受ける
        [property: JsonPropertyName("status")] string? Status
    );

    // --- クラスのフィールド ---
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ReplicateService> _logger; // Loggerを追加
    private readonly string _apiKey;
    private readonly string _modelVersion;
    private readonly string _apiUrl = "https://api.replicate.com/v1/predictions";
    private readonly JsonSerializerOptions _jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly string _negativePrompt;


    public ReplicateService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<ReplicateService> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _apiKey = configuration["REPLICATE_API_TOKEN"] ?? throw new InvalidOperationException("Configuration missing: REPLICATE_API_TOKEN");
        _modelVersion = "6afe2e6b27dad2d6f480b59195c221884b6acc589ff4d05ff0e5fc058690fbb9";

        // 推奨ネガティブプロンプトを設定
        _negativePrompt = "lowres, (bad), text, error, fewer, extra, missing, worst quality, jpeg artifacts, low quality, watermark, unfinished, displeasing, oldest, early, chromatic aberration, signature, extra digits, artistic error, username, scan, [abstract]";
    }

    /// <summary>
    /// Replicateで画像を生成し、そのバイナリデータとMIMEタイプを返します。
    /// </summary>
    public async Task<(byte[]? ImageBytes, string? MimeType)?> GenerateImageAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var httpClient = _httpClientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        string? pollingUrl = await StartPredictionAsync(httpClient, prompt, cancellationToken);
        if (string.IsNullOrEmpty(pollingUrl))
        {
            _logger.LogError("Failed to start prediction or get polling URL.");
            return null;
        }

        string? imageUrl = await PollForPredictionResultAsync(httpClient, pollingUrl, cancellationToken);
        if (string.IsNullOrEmpty(imageUrl))
        {
            _logger.LogError("Failed to get final image URL from polling.");
            return null;
        }

        try
        {
            _logger.LogInformation("Downloading image from URL: {ImageUrl}", imageUrl);
            var imageResponse = await httpClient.GetAsync(imageUrl, cancellationToken);
            if (!imageResponse.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to download image from {ImageUrl}. Status: {StatusCode}", imageUrl, imageResponse.StatusCode);
                return null;
            }

            var imageBytes = await imageResponse.Content.ReadAsByteArrayAsync(cancellationToken);
            var mimeType = imageResponse.Content.Headers.ContentType?.ToString() ?? "image/png"; // 不明な場合はpngに

            _logger.LogInformation("Image downloaded successfully. Size: {Size} bytes, MimeType: {MimeType}", imageBytes.Length, mimeType);
            return (imageBytes, mimeType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while downloading the generated image from {ImageUrl}", imageUrl);
            return null;
        }
    }

    // 予測を開始し、ポーリング用のURLを取得するメソッド
    private async Task<string?> StartPredictionAsync(HttpClient httpClient, string prompt, CancellationToken cancellationToken)
    {
        var requestBody = new ReplicateRequest(
            Version: _modelVersion,
            Input: new ReplicateInput(Prompt: prompt, NegativePrompt: _negativePrompt)
        );
        var jsonBody = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

        _logger.LogInformation("Starting image generation prediction with prompt: {Prompt}", prompt);

        try
        {
            // Preferヘッダーは付けずに、非同期でリクエストを投げる
            var response = await httpClient.PostAsync(_apiUrl, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Replicate API returned an error on initial request. Status: {StatusCode}, Response: {ErrorContent}", response.StatusCode, errorContent);
                return null;
            }

            var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var result = await JsonSerializer.DeserializeAsync<InitialResponse>(responseStream, _jsonSerializerOptions, cancellationToken);

            _logger.LogInformation("Prediction started. Polling URL: {PollingUrl}", result?.Urls?.Get);
            return result?.Urls?.Get;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An exception occurred while starting the prediction.");
            return null;
        }
    }

    // 予測結果をポーリングして最終的な画像URLを取得するメソッド
    private async Task<string?> PollForPredictionResultAsync(HttpClient httpClient, string pollingUrl, CancellationToken cancellationToken)
    {
        var pollingTimeout = TimeSpan.FromMinutes(3); // タイムアウトを3分に設定
        var pollingInterval = TimeSpan.FromSeconds(3); // ポーリング間隔を3秒に設定

        using var timeoutCts = new CancellationTokenSource(pollingTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        _logger.LogInformation("Polling for result at {PollingUrl}...", pollingUrl);

        while (!linkedCts.IsCancellationRequested)
        {
            try
            {
                var response = await httpClient.GetAsync(pollingUrl, linkedCts.Token);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Polling request failed with status code: {StatusCode}", response.StatusCode);
                    await Task.Delay(pollingInterval, linkedCts.Token);
                    continue;
                }

                var responseStream = await response.Content.ReadAsStreamAsync(linkedCts.Token);
                var result = await JsonSerializer.DeserializeAsync<PollingResponse>(responseStream, _jsonSerializerOptions, linkedCts.Token);

                switch (result?.Status)
                {
                    case "succeeded":
                        _logger.LogInformation("Prediction succeeded.");
                        // Outputが配列の場合、最初の要素を返す
                        if (result?.Output?.ValueKind == JsonValueKind.Array)
                        {
                            return result.Output.Value.EnumerateArray().FirstOrDefault().GetString();
                        }
                        // 文字列の場合もあるかもしれないので対応
                        return result?.Output?.ValueKind == JsonValueKind.String ? result.Output.Value.GetString() : null;

                    case "failed":
                    case "canceled":
                        _logger.LogError("Prediction {Status}.", result.Status);
                        return null;

                    default: // "starting" or "processing"
                        _logger.LogInformation("Prediction status is: {Status}. Waiting...", result?.Status ?? "unknown");
                        await Task.Delay(pollingInterval, linkedCts.Token);
                        break;
                }
            }
            catch (OperationCanceledException)
            {
                if (timeoutCts.IsCancellationRequested)
                {
                    _logger.LogError("Polling timed out after {PollingTimeout}.", pollingTimeout);
                }
                else
                {
                    _logger.LogInformation("Polling was canceled by the user.");
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An exception occurred during polling.");
                return null; // 不明なエラーでループを抜ける
            }
        }

        return null; // ループが正常に終了することは通常ないが念のため
    }
}