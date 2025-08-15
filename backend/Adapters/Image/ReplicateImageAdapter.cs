using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AiRoleplayChat.Backend.Application.Contracts;
using AiRoleplayChat.Backend.Application.Ports;
using AiRoleplayChat.Backend.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AiRoleplayChat.Backend.Adapters.Image;

/// <summary>
/// Replicate adapter implementing IImageModelPort for image generation
/// Wraps the existing Replicate API logic in hexagonal architecture
/// </summary>
public class ReplicateImageAdapter : IImageModelPort
{
    // API request/response internal records (migrated from ReplicateService)
    private record ReplicateInput(
        [property: JsonPropertyName("prompt")] string Prompt,
        [property: JsonPropertyName("negative_prompt")] string NegativePrompt
    );

    private record ReplicateRequest([property: JsonPropertyName("version")] string Version, [property: JsonPropertyName("input")] ReplicateInput Input);

    private record InitialResponse(
        [property: JsonPropertyName("urls")] Urls? Urls
    );
    private record Urls([property: JsonPropertyName("get")] string Get);

    private record PollingResponse(
        [property: JsonPropertyName("output")] JsonElement? Output,
        [property: JsonPropertyName("status")] string? Status
    );

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly IApiKeyService _apiKeyService;
    private readonly IUserSettingsService _userSettingsService;
    private readonly ILogger<ReplicateImageAdapter> _logger;
    private readonly string _defaultApiKey;
    private readonly string _defaultModelVersion;
    private readonly string _apiUrl = "https://api.replicate.com/v1/predictions";
    private readonly JsonSerializerOptions _jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly string _negativePrompt;

    public ReplicateImageAdapter(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IApiKeyService apiKeyService,
        IUserSettingsService userSettingsService,
        ILogger<ReplicateImageAdapter> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _config = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _apiKeyService = apiKeyService ?? throw new ArgumentNullException(nameof(apiKeyService));
        _userSettingsService = userSettingsService ?? throw new ArgumentNullException(nameof(userSettingsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _defaultApiKey = _config["REPLICATE_API_TOKEN"] ?? throw new InvalidOperationException("Configuration missing: REPLICATE_API_TOKEN");
        _defaultModelVersion = _config["Replicate:ImageGenerationVersion"] ?? "0fc0fa9885b284901a6f9c0b4d67701fd7647d157b88371427d63f8089ce140e";

        // Default negative prompt for better image quality
        _negativePrompt = "lowres, (bad), text, error, fewer, extra, missing, worst quality, jpeg artifacts, low quality, watermark, unfinished, displeasing, oldest, early, chromatic aberration, signature, extra digits, artistic error, username, scan, [abstract]";
    }

    /// <summary>
    /// Generate image using Replicate API
    /// </summary>
    public async Task<ImageResult?> GenerateImageAsync(ImageRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            string apiKey = _defaultApiKey;
            string modelVersion = _defaultModelVersion;

            // BYOK pattern - get user-specific API key and model if available
            if (request.UserId.HasValue)
            {
                var userApiKey = await _apiKeyService.GetApiKeyAsync(request.UserId.Value, "Replicate");
                if (!string.IsNullOrEmpty(userApiKey))
                {
                    apiKey = userApiKey;
                }

                var userSettings = await _userSettingsService.GetUserSettingsAsync(request.UserId.Value);
                var replicateModelSetting = userSettings.FirstOrDefault(s => s.ServiceType == "Replicate" && s.SettingKey == "ImageGenerationVersion");
                if (replicateModelSetting != null && !string.IsNullOrEmpty(replicateModelSetting.SettingValue))
                {
                    modelVersion = replicateModelSetting.SettingValue;
                }
            }

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            // Use provided negative prompt or default
            var finalNegativePrompt = request.NegativePrompt ?? _negativePrompt;

            // Start prediction
            string? pollingUrl = await StartPredictionAsync(httpClient, request.Prompt, finalNegativePrompt, modelVersion, cancellationToken);
            if (string.IsNullOrEmpty(pollingUrl))
            {
                _logger.LogError("Failed to start prediction or get polling URL for prompt: {Prompt}", request.Prompt);
                return null;
            }

            // Poll for result
            string? imageUrl = await PollForPredictionResultAsync(httpClient, pollingUrl, cancellationToken);
            if (string.IsNullOrEmpty(imageUrl))
            {
                _logger.LogError("Failed to get final image URL from polling for prompt: {Prompt}", request.Prompt);
                return null;
            }

            // Download image
            _logger.LogInformation("Downloading image from URL: {ImageUrl}", imageUrl);
            var imageResponse = await httpClient.GetAsync(imageUrl, cancellationToken);
            if (!imageResponse.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to download image from {ImageUrl}. Status: {StatusCode}", imageUrl, imageResponse.StatusCode);
                return null;
            }

            var imageBytes = await imageResponse.Content.ReadAsByteArrayAsync(cancellationToken);
            var mimeType = imageResponse.Content.Headers.ContentType?.ToString() ?? "image/png";

            _logger.LogInformation("Image generated successfully. Size: {Size} bytes, MimeType: {MimeType}", imageBytes.Length, mimeType);

            return new ImageResult(
                ImageBytes: imageBytes,
                MimeType: mimeType,
                ModelId: modelVersion,
                ServiceName: "Replicate",
                ActualPrompt: request.Prompt,
                Width: request.Width,
                Height: request.Height
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating image with Replicate for prompt: {Prompt}", request.Prompt);
            throw new InvalidOperationException($"Failed to generate image with Replicate: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Get capabilities of Replicate image model
    /// </summary>
    public ModelCapabilities GetCapabilities()
    {
        return new ModelCapabilities(
            ModelId: _defaultModelVersion,
            ServiceName: "Replicate",
            SupportsStreaming: false,
            SupportsImages: true,
            MaxTokens: null,
            SupportedFormats: new List<string> { "image/png", "image/jpeg" }
        );
    }

    private async Task<string?> StartPredictionAsync(HttpClient httpClient, string prompt, string negativePrompt, string modelVersion, CancellationToken cancellationToken)
    {
        var requestBody = new ReplicateRequest(
            Version: modelVersion,
            Input: new ReplicateInput(Prompt: prompt, NegativePrompt: negativePrompt)
        );
        var jsonBody = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

        _logger.LogInformation("Starting image generation prediction with prompt: {Prompt}", prompt);

        try
        {
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

    private async Task<string?> PollForPredictionResultAsync(HttpClient httpClient, string pollingUrl, CancellationToken cancellationToken)
    {
        var pollingTimeout = TimeSpan.FromMinutes(3);
        var pollingInterval = TimeSpan.FromSeconds(3);

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
                        // Output could be array or string
                        if (result?.Output?.ValueKind == JsonValueKind.Array)
                        {
                            return result.Output.Value.EnumerateArray().FirstOrDefault().GetString();
                        }
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
                return null;
            }
        }

        return null;
    }
}