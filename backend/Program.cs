using Google.Cloud.AIPlatform.V1;
using Google.Protobuf.WellKnownTypes;
using ProtoValue = Google.Protobuf.WellKnownTypes.Value; // Value に ProtoValue という別名を付ける
using AiRoleplayChat.Backend.Models;

// CORSポリシー名を定義
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

var builder = WebApplication.CreateBuilder(args);

// --- サービス登録 ---
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins, policy =>
    {
        policy.WithOrigins("http://localhost:5173") // フロントエンドの開発サーバー
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddHttpClient(); // HttpClient サービスを登録
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNameCaseInsensitive = true; // JSONプロパティ名の大文字小文字を無視
});

builder.Services.AddEndpointsApiExplorer();
// builder.Services.AddSwaggerGen(); // Swagger を使用する場合はコメント解除

var app = builder.Build();

// --- HTTPリクエストパイプラインの設定 ---
app.UseHttpsRedirection();
app.UseCors(MyAllowSpecificOrigins); // CORSミドルウェアを使用

// --- APIエンドポイントの定義 ---

// チャットAPI: Gemini API を使用してプロンプトに応答
app.MapPost("/api/chat", async (ChatRequest request, IHttpClientFactory clientFactory, IConfiguration config) =>
{
    Console.WriteLine($"Received prompt: {request.Prompt}");

    // Gemini API の設定
    var apiKey = config["Gemini:ApiKey"] ?? throw new InvalidOperationException("Gemini APIキーが設定されていません。");
    var model = config["Gemini:ChatModel"] ?? "gemini-1.5-flash-latest";
    var geminiApiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

    var httpClient = clientFactory.CreateClient();

    // リクエストボディを作成
    var geminiRequest = new GeminiApiRequest
    {
        Contents = new[] {
            new GeminiContent {
                Parts = new[] {
                    new GeminiPart { Text = request.Prompt }
                }
            }
        },
        GenerationConfig = new GeminiGenerationConfig
        {
            Temperature = config.GetValue<double?>("Gemini:DefaultTemperature") ?? 0.7,
            MaxOutputTokens = config.GetValue<int?>("Gemini:DefaultMaxOutputTokens") ?? 100
        }
    };

    try
    {
        // Gemini API へリクエストを送信
        var response = await httpClient.PostAsJsonAsync(geminiApiUrl, geminiRequest);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            Console.Error.WriteLine($"Gemini API Error: {response.StatusCode} - {errorContent}");
            return Results.Problem($"Gemini APIとの通信に失敗しました: {response.StatusCode}", statusCode: (int)response.StatusCode);
        }

        // レスポンスを処理
        var geminiResponse = await response.Content.ReadFromJsonAsync<GeminiApiResponse>();
        var replyText = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text ?? "応答が取得できませんでした。";

        return Results.Ok(new ChatResponse(replyText));
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Gemini API 呼び出し中にエラー: {ex.Message}");
        return Results.Problem($"エラーが発生しました: {ex.Message}", statusCode: 500);
    }
}).WithName("PostChatMessage");

// 画像生成API: Vertex AI Imagen を使用
app.MapPost("/api/test-imagen", async (ImageGenRequest request, IHttpClientFactory clientFactory, IConfiguration config) =>
{
    try
    {
        string japanesePrompt = request.Prompt;
        if (string.IsNullOrWhiteSpace(japanesePrompt))
        {
            return Results.BadRequest(new { Message = "プロンプトが空です。" });
        }

        // --- 1. Gemini API で日本語プロンプトを英語に翻訳 ---
        Console.WriteLine($"Translating Japanese prompt: \"{japanesePrompt}\" using Gemini API...");
        string englishPrompt = string.Empty;

        var apiKey = config["Gemini:ApiKey"] ?? throw new InvalidOperationException("Gemini APIキーが設定されていません。");
        var translationModel = config["Gemini:TranslationModel"] ?? "gemini-1.5-flash-latest";
        var geminiApiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{translationModel}:generateContent?key={apiKey}";
        var httpClient = clientFactory.CreateClient();

        var translationInstruction = $"Translate the following Japanese text into English: \"{japanesePrompt}\"";

        var geminiRequest = new GeminiApiRequest
        {
            Contents = new[] { new GeminiContent { Parts = new[] { new GeminiPart { Text = translationInstruction } } } },
            GenerationConfig = new GeminiGenerationConfig
            {
                Temperature = config.GetValue<double?>("Gemini:TranslationTemperature") ?? 0.2,
                MaxOutputTokens = config.GetValue<int?>("Gemini:TranslationMaxOutputTokens") ?? 256
            }
        };

        var geminiResponseRaw = await httpClient.PostAsJsonAsync(geminiApiUrl, geminiRequest);
        if (!geminiResponseRaw.IsSuccessStatusCode)
        {
            var errorContent = await geminiResponseRaw.Content.ReadAsStringAsync();
            Console.Error.WriteLine($"Gemini API Error: {geminiResponseRaw.StatusCode} - {errorContent}");
            return Results.Problem("Gemini API で翻訳に失敗しました。", statusCode: 500);
        }

        var geminiResponse = await geminiResponseRaw.Content.ReadFromJsonAsync<GeminiApiResponse>();
        englishPrompt = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(englishPrompt))
        {
            return Results.Problem("翻訳結果が空です。", statusCode: 500);
        }

        // --- 2. 翻訳されたプロンプトで Vertex AI Imagen API を呼び出し ---
        var projectId = config["VertexAi:ProjectId"] ?? throw new InvalidOperationException("VertexAi:ProjectId が設定されていません。");
        var location = config["VertexAi:Location"] ?? throw new InvalidOperationException("VertexAi:Location が設定されていません。");
        var modelId = config["VertexAi:ImageGenerationModel"] ?? throw new InvalidOperationException("VertexAi:ImageGenerationModel が設定されていません。");

        var endpointName = EndpointName.FromProjectLocationPublisherModel(projectId, location, "google", modelId);
        var predictionServiceClient = CreatePredictionServiceClient(config);

        var instances = new List<ProtoValue>
        {
            ProtoValue.ForStruct(new Struct
            {
                Fields =
                {
                    { "prompt", ProtoValue.ForString(englishPrompt) },
                    { "sampleCount", ProtoValue.ForNumber(1) }
                }
            })
        };

        var imagenResponse = await predictionServiceClient.PredictAsync(endpointName, instances, null);
        if (imagenResponse.Predictions.Count > 0)
        {
            var firstPrediction = imagenResponse.Predictions[0];
            if (firstPrediction.KindCase == ProtoValue.KindOneofCase.StructValue &&
                firstPrediction.StructValue.Fields.TryGetValue("bytesBase64Encoded", out var base64Value) &&
                base64Value.KindCase == ProtoValue.KindOneofCase.StringValue &&
                firstPrediction.StructValue.Fields.TryGetValue("mimeType", out var mimeTypeValue) &&
                mimeTypeValue.KindCase == ProtoValue.KindOneofCase.StringValue)
            {
                var imageResponse = new ImageGenerationResponse(mimeTypeValue.StringValue, base64Value.StringValue);
                return Results.Ok(imageResponse);
            }
        }

        return Results.Problem("画像生成に失敗しました。");
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error in /api/test-imagen endpoint: {ex.Message}");
        return Results.Problem($"エラーが発生しました: {ex.Message}", statusCode: 500);
    }
}).WithName("TestImagen");

app.Run();

// Vertex AI クライアントを生成するヘルパーメソッド
PredictionServiceClient CreatePredictionServiceClient(IConfiguration config)
{
    string location = config["VertexAi:PredictionEndpointLocation"] ?? "us-central1";
    string endpoint = $"{location}-aiplatform.googleapis.com";

    return new PredictionServiceClientBuilder
    {
        Endpoint = endpoint
    }.Build();
}