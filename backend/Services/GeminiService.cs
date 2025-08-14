using AiRoleplayChat.Backend.Domain.Entities;
using AiRoleplayChat.Backend.Models; // モデルクラスを使う
using System.Text.Json; // JsonSerializerOptions を使う

namespace AiRoleplayChat.Backend.Services; // 名前空間を確認・調整

public class GeminiService : IGeminiService // IGeminiService インターフェースを実装
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly IApiKeyService _apiKeyService;
    private readonly IUserSettingsService _userSettingsService;
    private readonly string _defaultApiKey; // システム用デフォルトAPIキーを保持 (コンストラクタで必須チェック)
    private readonly JsonSerializerOptions _jsonSerializerOptions; // JSON設定を保持

    // コンストラクタで HttpClientFactory と IConfiguration を受け取る
    public GeminiService(IHttpClientFactory httpClientFactory, IConfiguration configuration, IApiKeyService apiKeyService, IUserSettingsService userSettingsService)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _config = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _apiKeyService = apiKeyService ?? throw new ArgumentNullException(nameof(apiKeyService));
        _userSettingsService = userSettingsService ?? throw new ArgumentNullException(nameof(userSettingsService));

        // APIキーはサービス生成時にチェック（設定がなければ起動時にエラーにする）
        _defaultApiKey = _config["Gemini:ApiKey"] ?? throw new InvalidOperationException("Configuration missing: Gemini:ApiKey");

        // JSONシリアライザのオプション (Program.cs で設定したものと同様の設定が望ましい)
        _jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true // 大文字小文字を無視
            // 他にも必要なオプションがあれば追加
        };
    }

    /// <summary>
    /// 指定されたプロンプトに対するチャット応答を生成します。
    /// </summary>
    public async Task<string> GenerateChatResponseAsync(string prompt, string systemPrompt, List<ChatMessage> history, int? userId = null, CancellationToken cancellationToken = default)
    {
        var defaultModel = _config["Gemini:ChatModel"] ?? "gemini-1.5-flash-latest";
        var model = await GetUserSpecificModelAsync(userId, "ChatModel", defaultModel);
        
        var generationConfig = new GeminiGenerationConfig
        {
            Temperature = _config.GetValue<double?>("Gemini:DefaultTemperature") ?? 0.7,
            MaxOutputTokens = _config.GetValue<int?>("Gemini:DefaultMaxOutputTokens") ?? 1024
        };

        const int MaxHistoryCount = 30;

        // 共通メソッドを呼び出す
        return await CallGeminiApiAsync(model, prompt, systemPrompt, history, MaxHistoryCount, generationConfig, userId, cancellationToken);
    }

    // --- Gemini API を呼び出す共通プライベートメソッド ---
    private async Task<string> CallGeminiApiAsync(string modelName, string promptText, string? systemPrompt, List<ChatMessage> history, int MaxHistoryCount, GeminiGenerationConfig generationConfig, int? userId, CancellationToken cancellationToken)
    {
        // ユーザー専用APIキーを取得、なければデフォルトを使用
        string apiKey = _defaultApiKey;
        if (userId.HasValue)
        {
            var userApiKey = await _apiKeyService.GetApiKeyAsync(userId.Value, "Gemini");
            if (!string.IsNullOrEmpty(userApiKey))
            {
                apiKey = userApiKey;
            }
        }

        // APIキーはコンストラクタで取得・チェック済み
        var geminiApiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:generateContent?key={apiKey}";
        var httpClient = _httpClientFactory.CreateClient(); // HttpClientを取得

        // --- ★ Contents 配列の構築 (履歴を考慮) ---
        var contents = new List<GeminiContent>();

        // 1. 履歴を Contents 形式に変換して追加 (トークン数制限を考慮 - まずは件数制限)
        var recentHistory = history.OrderByDescending(h => h.Timestamp).Take(MaxHistoryCount).Reverse(); // 新しい順に N 件取得し、再度古い順に戻す

        foreach (var message in recentHistory)
        {
            contents.Add(new GeminiContent
            {
                Role = message.Sender == "user" ? "user" : "model",
                Parts = new[] { new GeminiPart { Text = message.Text } }
            });
        }

        // セーフティ設定を定義
        var safetySettings = new[]
        {
            new GeminiSafetySetting { Category = "HARM_CATEGORY_SEXUALLY_EXPLICIT", Threshold = "BLOCK_NONE" },
            new GeminiSafetySetting { Category = "HARM_CATEGORY_HARASSMENT", Threshold = "BLOCK_NONE" },
            new GeminiSafetySetting { Category = "HARM_CATEGORY_DANGEROUS_CONTENT", Threshold = "BLOCK_NONE" }
        };

        // 2. 新しいユーザープロンプトを追加
        contents.Add(new GeminiContent
        {
            Role = "user", // 最後の発言は常に user
            Parts = new[] { new GeminiPart { Text = promptText } }
        });

        var geminiRequest = new GeminiApiRequest
        {
            Contents = contents.ToArray(),
            GenerationConfig = generationConfig,
            SystemInstruction = !string.IsNullOrWhiteSpace(systemPrompt)
            ? new GeminiContent { Parts = new[] { new GeminiPart { Text = systemPrompt } } }
            : null,
            SafetySettings = safetySettings
        };

        HttpResponseMessage responseRaw;
        string rawJsonResponseBody;

        try
        {
            // API へ POST リクエストを送信
            responseRaw = await httpClient.PostAsJsonAsync(geminiApiUrl, geminiRequest, _jsonSerializerOptions, cancellationToken);
            rawJsonResponseBody = await responseRaw.Content.ReadAsStringAsync(cancellationToken);

            // デバッグ用にリクエスト内容とレスポンスを出力してもよい
            Console.WriteLine($"[DEBUG] Gemini Request to {modelName}: {JsonSerializer.Serialize(geminiRequest, _jsonSerializerOptions)}");
            Console.WriteLine($"[DEBUG] Raw Gemini Response from {modelName}: {rawJsonResponseBody}");

        }
        catch (Exception ex) when (ex is HttpRequestException || ex is OperationCanceledException || ex is JsonException)
        {
            // ネットワークエラー、キャンセル、リクエスト作成時のJSONエラーなど
            Console.Error.WriteLine($"Error calling Gemini API ({modelName}): {ex.GetType().Name} - {ex.Message}");
            // より具体的なエラー情報をラップして再スロー
            throw new Exception($"Failed to communicate with Gemini API ({modelName}). See inner exception for details.", ex);
        }

        // HTTPステータスコードがエラーの場合
        if (!responseRaw.IsSuccessStatusCode)
        {
            Console.Error.WriteLine($"Gemini API Error ({modelName}): {responseRaw.StatusCode} - {rawJsonResponseBody}");
            // エラー応答の内容も含めて例外をスロー
            throw new Exception($"Gemini API returned error ({modelName}): {responseRaw.StatusCode}. Response: {rawJsonResponseBody}");
        }

        try
        {
            // 成功応答をデシリアライズ
            var geminiResponse = JsonSerializer.Deserialize<GeminiApiResponse>(rawJsonResponseBody, _jsonSerializerOptions);

            // 応答テキストを取得 (nullチェックとTrimを追加)
            string? resultText = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text?.Trim();

            if (string.IsNullOrEmpty(resultText))
            {
                // 応答は成功したが、期待するテキストが含まれていない場合 (安全フィルター等)
                Console.Error.WriteLine($"Gemini API ({modelName}) returned empty content. Raw response: {rawJsonResponseBody}");
                throw new Exception($"Gemini API ({modelName}) returned empty or invalid content. Check for safety filters or prompt issues.");
            }

            Console.WriteLine($"Gemini API call successful for model {modelName}.");
            return resultText; // 成功した場合はテキストを返す
        }
        catch (JsonException ex) // デシリアライズ失敗
        {
            Console.Error.WriteLine($"Failed to deserialize Gemini API response ({modelName}): {ex.Message}. Raw response: {rawJsonResponseBody}");
            throw new Exception($"Failed to parse response from Gemini API ({modelName}).", ex);
        }
    }

    public async Task<string> GenerateImagePromptAsync(
        CharacterProfile character,
        List<ChatMessage> history,
        int? userId = null,
        CancellationToken cancellationToken = default)
    {
        var defaultModel = _config["Gemini:TranslationModel"] ?? "gemini-1.5-flash-latest";
        var model = await GetUserSpecificModelAsync(userId, "ImagePromptGenerationModel", defaultModel);

        var generationConfig = new GeminiGenerationConfig
        {
            Temperature = _config.GetValue<double?>("Gemini:TranslationTemperature") ?? 0.4, // 少し創造性を上げる
            MaxOutputTokens = _config.GetValue<int?>("Gemini:TranslationMaxOutputTokens") ?? 256
        };

        // Get user-specific image prompt instruction or use default
        string imagePromptInstructionTemplate = await GetUserSpecificImagePromptInstructionAsync(userId);
        
        // Replace character placeholders in the instruction template
        string imagePromptInstruction = imagePromptInstructionTemplate
            .Replace("{character.Name}", character.Name)
            .Replace("{character.Personality}", character.Personality)
            .Replace("{character.Backstory}", character.Backstory);

        const int MaxHistoryCount = 6;

        // CallGeminiApiAsyncを呼び出す
        // systemPromptとして新しい指示を渡し、ユーザープロンプトは空でOK
        return await CallGeminiApiAsync(model, "", imagePromptInstruction, history, MaxHistoryCount, generationConfig, userId, cancellationToken);
    }

    private async Task<string> GetUserSpecificModelAsync(int? userId, string settingKey, string defaultValue)
    {
        if (!userId.HasValue)
        {
            return defaultValue;
        }

        var userSettings = await _userSettingsService.GetUserSettingsAsync(userId.Value);
        var modelSetting = userSettings.FirstOrDefault(s => s.ServiceType == "Gemini" && s.SettingKey == settingKey);

        if (modelSetting != null && !string.IsNullOrEmpty(modelSetting.SettingValue))
        {
            return modelSetting.SettingValue;
        }

        return defaultValue;
    }

    private async Task<string> GetUserSpecificImagePromptInstructionAsync(int? userId)
    {
        // Default instruction template with placeholders
        string defaultInstruction = 
            "You are an expert in creating high-quality, Danbooru-style prompts for the Animagine XL 3.1 image generation model. " +
            "Based on the provided Character Profile and conversation history, generate a single, concise English prompt.\n\n" +

            "## Character Profile:\n" +
            "- Name: {character.Name}\n" +
            "- Personality & Appearance: {character.Personality}\n" +
            "- Backstory & Other traits: {character.Backstory}\n\n" +

            "## Prompt Generation Rules:\n" +
            "1. **Tag-Based Only:** The entire prompt must be a series of comma-separated tags.\n" +
            "2. **Mandatory Prefixes:** ALWAYS start the prompt with: `masterpiece, best quality, very aesthetic, absurdres`.\n" +

            "3. **Rating Modifier:** Immediately after the prefixes, you MUST add ONE of the following rating tags based on the conversation's context. \n" +
            "   - `safe`: For wholesome or everyday scenes. (This is the default if unsure).\n" +
            "   - `sensitive`: For slightly suggestive content, artistic nudity, swimwear, or mild violence.\n" +
            "   - `nsfw`: For explicit themes, non-explicit nudity, or strong violence.\n" +
            "   - `explicit`: For pornographic content or extreme violence/gore. Use this for 'Explicit' level content.\n\n" +

            "4. **Year Modifier (Optional):** If the context suggests a specific era (e.g., retro, modern), you can add ONE of the following: `newest`, `recent`, `mid`, `early`, `oldest`.\n" +

            "5. **Core Content (Tag Order Matters):** Structure the main part of the prompt in this order:\n" +
            "   - Subject (e.g., `1girl`, `2boys`).\n" +
            "   - Character details from the profile (e.g., `long blonde hair`, `blue eyes`).\n" +
            "   - Scene details from the last message (clothing, pose, emotion, background, e.g., `wearing a school uniform`, `sitting on a park bench`, `smiling`, `night`, `rain`).\n" +

            "6. **Final Output:** Do not include any explanation or markdown. Only the final comma-separated prompt.\n\n" +

            "## Example Output:\n" +
            "masterpiece, best quality, very aesthetic, absurdres, safe, newest, 1girl, amelia, from_my_novel, long blonde hair, blue eyes, smiling, wearing a school uniform, sitting on a park bench, sunny day, cherry blossoms";

        if (!userId.HasValue)
        {
            return defaultInstruction;
        }

        var userSettings = await _userSettingsService.GetUserSettingsAsync(userId.Value);
        var instructionSetting = userSettings.FirstOrDefault(s => s.ServiceType == "Gemini" && s.SettingKey == "ImagePromptInstruction");

        if (instructionSetting != null && !string.IsNullOrEmpty(instructionSetting.SettingValue))
        {
            return instructionSetting.SettingValue;
        }

        return defaultInstruction;
    }
}