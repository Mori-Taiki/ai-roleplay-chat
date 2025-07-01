using AiRoleplayChat.Backend.Domain.Entities;
using AiRoleplayChat.Backend.Models; // モデルクラスを使う
using System.Text.Json; // JsonSerializerOptions を使う

namespace AiRoleplayChat.Backend.Services; // 名前空間を確認・調整

public class GeminiService : IGeminiService // IGeminiService インターフェースを実装
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly string _apiKey; // APIキーを保持 (コンストラクタで必須チェック)
    private readonly JsonSerializerOptions _jsonSerializerOptions; // JSON設定を保持

    // コンストラクタで HttpClientFactory と IConfiguration を受け取る
    public GeminiService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _config = configuration ?? throw new ArgumentNullException(nameof(configuration));

        // APIキーはサービス生成時にチェック（設定がなければ起動時にエラーにする）
        _apiKey = _config["Gemini:ApiKey"] ?? throw new InvalidOperationException("Configuration missing: Gemini:ApiKey");

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
    public async Task<string> GenerateChatResponseAsync(string prompt, string systemPrompt, List<ChatMessage> history, CancellationToken cancellationToken = default)
    {
        // 設定ファイルからチャット用の設定を読み込む
        var model = _config["Gemini:ChatModel"] ?? "gemini-1.5-flash-latest";
        var generationConfig = new GeminiGenerationConfig
        {
            Temperature = _config.GetValue<double?>("Gemini:DefaultTemperature") ?? 0.7,
            MaxOutputTokens = _config.GetValue<int?>("Gemini:DefaultMaxOutputTokens") ?? 1024
        };

        // 共通メソッドを呼び出す
        return await CallGeminiApiAsync(model, prompt, systemPrompt, history, generationConfig, cancellationToken);
    }

    /// <summary>
    /// 指定された日本語テキストを画像生成に適した英語プロンプトに翻訳します。
    /// </summary>
    public async Task<string> TranslateToEnglishAsync(string japaneseText, CancellationToken cancellationToken = default)
    {
        // 設定ファイルから翻訳用の設定を読み込む
        var model = _config["Gemini:TranslationModel"] ?? "gemini-1.5-flash-latest";
        var generationConfig = new GeminiGenerationConfig
        {
            Temperature = _config.GetValue<double?>("Gemini:TranslationTemperature") ?? 0.2,
            MaxOutputTokens = _config.GetValue<int?>("Gemini:TranslationMaxOutputTokens") ?? 256
        };

        // 必要なら、より画像生成向けにする指示を追加
        string translationInstruction = $"Translate the following Japanese text into a detailed English prompt suitable for an image generation AI (like Imagen). Focus on descriptive nouns and adjectives.";

        // 共通メソッドを呼び出す
        return await CallGeminiApiAsync(model, japaneseText, translationInstruction, new List<ChatMessage>(), generationConfig, cancellationToken);
    }

    // --- Gemini API を呼び出す共通プライベートメソッド ---
    private async Task<string> CallGeminiApiAsync(string modelName, string promptText, string? systemPrompt, List<ChatMessage> history, GeminiGenerationConfig generationConfig, CancellationToken cancellationToken)
    {
        // APIキーはコンストラクタで取得・チェック済み
        var geminiApiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:generateContent?key={_apiKey}";
        var httpClient = _httpClientFactory.CreateClient(); // HttpClientを取得

        // --- ★ Contents 配列の構築 (履歴を考慮) ---
        var contents = new List<GeminiContent>();

        // 1. 履歴を Contents 形式に変換して追加 (トークン数制限を考慮 - まずは件数制限)
        const int MaxHistoryCount = 10;
        var recentHistory = history.OrderByDescending(h => h.Timestamp).Take(MaxHistoryCount).Reverse(); // 新しい順に N 件取得し、再度古い順に戻す

        foreach (var message in recentHistory)
        {
            contents.Add(new GeminiContent
            {
                Role = message.Sender == "user" ? "user" : "model",
                Parts = new[] { new GeminiPart { Text = message.Text } }
                // TODO: 画像メッセージの扱い (ImageUrl がある場合、Part に画像データを含めるか？ - Gemini API の仕様確認)
            });
        }

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
        : null
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

    public async Task<string> GenerateImagePromptAsync(List<ChatMessage> history, CancellationToken cancellationToken = default)
    {
        var model = _config["Gemini:TranslationModel"] ?? "gemini-2.5-flash-preview-05-20"; // 翻訳・要約向けのモデルを使用
        var generationConfig = new GeminiGenerationConfig
        {
            Temperature = _config.GetValue<double?>("Gemini:TranslationTemperature") ?? 0.3, // 少し創造性を持たせる
            MaxOutputTokens = _config.GetValue<int?>("Gemini:TranslationMaxOutputTokens") ?? 256
        };

        // Geminiに与える指示（システムプロンプト）
        string imagePromptInstruction =
            "You are an expert in creating high-quality image generation prompts. " +
            "Based on the following conversation history, generate a single, concise English prompt for an image generation AI (like Stable Diffusion or Imagen). " +
            "The prompt should capture the essence of the last AI's message, including the character's actions, emotions, and the surrounding scene. " +
            "The prompt must be in English, tag-based, and comma-separated. " +
            "Always start the prompt with 'masterpiece, best quality, very aesthetic, absurdres'. Do not include any other text or explanation. " +
            "Example output: masterpiece, best quality, very aesthetic, absurdres, 1girl, solo, smile, long blonde hair, school uniform, sitting on a park bench, sunny day, cherry blossoms";

        // CallGeminiApiAsyncを呼び出す
        // 最後のユーザープロンプトは不要なので空文字を渡す
        return await CallGeminiApiAsync(model, "", imagePromptInstruction, history, generationConfig, cancellationToken);
    }
}