using System.Text.Json;
using AiRoleplayChat.Backend.Application.Contracts;
using AiRoleplayChat.Backend.Application.Ports;
using AiRoleplayChat.Backend.Data;
using AiRoleplayChat.Backend.Domain.Entities;
using AiRoleplayChat.Backend.Models;
using AiRoleplayChat.Backend.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AiRoleplayChat.Backend.Adapters.Text;

/// <summary>
/// Gemini adapter implementing ITextModelPort for text generation
/// Wraps the existing Gemini API logic in hexagonal architecture
/// </summary>
public class GeminiTextAdapter : ITextModelPort
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly IApiKeyService _apiKeyService;
    private readonly AppDbContext _context;
    private readonly ILogger<GeminiTextAdapter> _logger;
    private readonly string _defaultApiKey;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public GeminiTextAdapter(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IApiKeyService apiKeyService,
        AppDbContext context,
        ILogger<GeminiTextAdapter> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _config = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _apiKeyService = apiKeyService ?? throw new ArgumentNullException(nameof(apiKeyService));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _defaultApiKey = _config["Gemini:ApiKey"] ?? throw new InvalidOperationException("Configuration missing: Gemini:ApiKey");

        _jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    /// <summary>
    /// Generate text completion using Gemini API
    /// </summary>
    public async Task<TextCompletion> GenerateTextAsync(TextRequest request, CancellationToken cancellationToken = default)
    {
        var defaultModel = _config["Gemini:ChatModel"] ?? "gemini-1.5-flash-latest";
        var model = await GetUserSpecificModelAsync(request.UserId, "ChatModel", defaultModel);

        var generationConfig = new GeminiGenerationConfig
        {
            Temperature = request.Temperature ?? _config.GetValue<double?>("Gemini:DefaultTemperature") ?? 0.7,
            MaxOutputTokens = request.MaxTokens ?? _config.GetValue<int?>("Gemini:DefaultMaxOutputTokens") ?? 1024
        };

        try
        {
            string resultText = await CallGeminiApiAsync(
                model,
                request.Prompt,
                request.SystemPrompt,
                request.History ?? new List<ChatTurn>(),
                30, // MaxHistoryCount
                generationConfig,
                request.UserId,
                cancellationToken);

            return new TextCompletion(
                Text: resultText,
                ModelId: model,
                ServiceName: "Gemini",
                TokensUsed: null, // Gemini doesn't return token usage in response
                ConfidenceScore: null
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating text with Gemini model {Model}", model);
            throw new InvalidOperationException($"Failed to generate text with Gemini: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Get capabilities of Gemini text model
    /// </summary>
    public ModelCapabilities GetCapabilities()
    {
        var defaultModel = _config["Gemini:ChatModel"] ?? "gemini-1.5-flash-latest";
        
        return new ModelCapabilities(
            ModelId: defaultModel,
            ServiceName: "Gemini",
            SupportsStreaming: false,
            SupportsImages: false,
            MaxTokens: _config.GetValue<int?>("Gemini:DefaultMaxOutputTokens") ?? 1024,
            SupportedFormats: new List<string> { "text" }
        );
    }

    private async Task<string> GetUserSpecificModelAsync(int? userId, string settingKey, string defaultValue)
    {
        if (!userId.HasValue)
        {
            return defaultValue;
        }

        var user = await _context.Users
            .Include(u => u.AiSettings)
            .FirstOrDefaultAsync(u => u.Id == userId.Value);

        if (user?.AiSettings != null)
        {
            var aiSettings = user.AiSettings;
            return settingKey switch
            {
                "ChatModel" => aiSettings.ChatGenerationModel ?? defaultValue,
                "ImagePromptGenerationModel" => aiSettings.ImagePromptGenerationModel ?? defaultValue,
                _ => defaultValue
            };
        }

        return defaultValue;
    }

    private async Task<string> CallGeminiApiAsync(
        string modelName,
        string promptText,
        string? systemPrompt,
        List<ChatTurn> history,
        int maxHistoryCount,
        GeminiGenerationConfig generationConfig,
        int? userId,
        CancellationToken cancellationToken)
    {
        // Get API key (BYOK pattern)
        string apiKey = _defaultApiKey;
        if (userId.HasValue)
        {
            var userApiKey = await _apiKeyService.GetApiKeyAsync(userId.Value, "Gemini");
            if (!string.IsNullOrEmpty(userApiKey))
            {
                apiKey = userApiKey;
            }
        }

        var geminiApiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:generateContent?key={apiKey}";
        var httpClient = _httpClientFactory.CreateClient();

        // Build contents array from history
        var contents = new List<GeminiContent>();

        // Convert ChatTurn history to Gemini format (take recent items, reverse to chronological order)
        var recentHistory = history.TakeLast(maxHistoryCount);

        foreach (var turn in recentHistory)
        {
            // Add user message
            contents.Add(new GeminiContent
            {
                Role = "user",
                Parts = new[] { new GeminiPart { Text = turn.UserMessage } }
            });

            // Add assistant message if exists
            if (!string.IsNullOrEmpty(turn.AssistantMessage))
            {
                contents.Add(new GeminiContent
                {
                    Role = "model",
                    Parts = new[] { new GeminiPart { Text = turn.AssistantMessage } }
                });
            }
        }

        // Add current user prompt
        contents.Add(new GeminiContent
        {
            Role = "user",
            Parts = new[] { new GeminiPart { Text = promptText } }
        });

        // Safety settings
        var safetySettings = new[]
        {
            new GeminiSafetySetting { Category = "HARM_CATEGORY_SEXUALLY_EXPLICIT", Threshold = "BLOCK_NONE" },
            new GeminiSafetySetting { Category = "HARM_CATEGORY_HARASSMENT", Threshold = "BLOCK_NONE" },
            new GeminiSafetySetting { Category = "HARM_CATEGORY_DANGEROUS_CONTENT", Threshold = "BLOCK_NONE" }
        };

        var geminiRequest = new GeminiApiRequest
        {
            Contents = contents.ToArray(),
            GenerationConfig = generationConfig,
            SystemInstruction = !string.IsNullOrWhiteSpace(systemPrompt)
                ? new GeminiContent { Parts = new[] { new GeminiPart { Text = systemPrompt } } }
                : null,
            SafetySettings = safetySettings
        };

        try
        {
            var responseRaw = await httpClient.PostAsJsonAsync(geminiApiUrl, geminiRequest, _jsonSerializerOptions, cancellationToken);
            var rawJsonResponseBody = await responseRaw.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogDebug("Gemini Request to {ModelName}: {Request}", modelName, JsonSerializer.Serialize(geminiRequest, _jsonSerializerOptions));
            _logger.LogDebug("Raw Gemini Response from {ModelName}: {Response}", modelName, rawJsonResponseBody);

            if (!responseRaw.IsSuccessStatusCode)
            {
                _logger.LogError("Gemini API Error ({ModelName}): {StatusCode} - {Response}", modelName, responseRaw.StatusCode, rawJsonResponseBody);
                throw new InvalidOperationException($"Gemini API returned error ({modelName}): {responseRaw.StatusCode}. Response: {rawJsonResponseBody}");
            }

            var geminiResponse = JsonSerializer.Deserialize<GeminiApiResponse>(rawJsonResponseBody, _jsonSerializerOptions);
            var resultText = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text?.Trim();

            if (string.IsNullOrEmpty(resultText))
            {
                throw new InvalidOperationException($"Empty or null response from Gemini API ({modelName})");
            }

            return resultText;
        }
        catch (Exception ex) when (ex is HttpRequestException || ex is OperationCanceledException || ex is JsonException)
        {
            _logger.LogError(ex, "Error calling Gemini API ({ModelName}): {ExceptionType} - {Message}", modelName, ex.GetType().Name, ex.Message);
            throw new InvalidOperationException($"Failed to communicate with Gemini API ({modelName}). See inner exception for details.", ex);
        }
    }
}