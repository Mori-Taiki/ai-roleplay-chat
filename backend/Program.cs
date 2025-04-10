using Google.Cloud.AIPlatform.V1;
using Google.Protobuf.WellKnownTypes;
using ProtoValue = Google.Protobuf.WellKnownTypes.Value; // Value に ProtoValue という別名を付ける
using AiRoleplayChat.Backend.Models;
using AiRoleplayChat.Backend.Services; 

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

builder.Services.AddScoped<IGeminiService, GeminiService>();
builder.Services.AddSingleton(provider =>
{
    // IConfiguration をDIコンテナから取得
    var config = provider.GetRequiredService<IConfiguration>();
    // 設定ファイルからエンドポイントの場所を取得してクライアントを生成
    string location = config["VertexAi:PredictionEndpointLocation"] ?? throw new InvalidOperationException("Configuration missing: VertexAi:PredictionEndpointLocation");
    string endpoint = $"{location}-aiplatform.googleapis.com";
    Console.WriteLine($"[DI] Creating Singleton PredictionServiceClient for endpoint: {endpoint}");
    return new PredictionServiceClientBuilder { Endpoint = endpoint }.Build();
});
builder.Services.AddScoped<IImagenService, ImagenService>();
builder.Services.AddEndpointsApiExplorer();
// builder.Services.AddSwaggerGen(); // Swagger を使用する場合はコメント解除

var app = builder.Build();

// --- HTTPリクエストパイプラインの設定 ---
app.UseHttpsRedirection();
app.UseCors(MyAllowSpecificOrigins); // CORSミドルウェアを使用

// --- APIエンドポイントの定義 ---

// チャットAPI: Gemini API を使用してプロンプトに応答
app.MapPost("/api/chat", async (ChatRequest request, IGeminiService geminiService /* ★ 引数変更 */ ) =>
{
    Console.WriteLine($"Received prompt: {request.Prompt}");
    try
    {
        // GeminiService のメソッドを呼び出して応答を生成
        string replyText = await geminiService.GenerateChatResponseAsync(request.Prompt);
        return Results.Ok(new ChatResponse(replyText));
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error in /api/chat endpoint calling GeminiService: {ex.Message}");
        return Results.Problem($"チャット応答の生成中にエラーが発生しました。", statusCode: 500);
    }
}).WithName("PostChatMessage");


// 画像生成API: Vertex AI Imagen を使用
app.MapPost("/api/test-imagen", async (ImageGenRequest request, IGeminiService geminiService, IImagenService imagenService) =>
{
    try
    {
        string japanesePrompt = request.Prompt;
        if (string.IsNullOrWhiteSpace(japanesePrompt))
        {
            return Results.BadRequest(new { Message = "プロンプトが空です。" });
        }

        // --- 1. 翻訳 (GeminiService を使う) ---
        string englishPrompt;
        try
        {
            // GeminiService のメソッドを呼び出す
            Console.WriteLine($"Translating Japanese prompt: \"{japanesePrompt}\" using GeminiService...");
            englishPrompt = await geminiService.TranslateToEnglishAsync(japanesePrompt);
            Console.WriteLine($"Translation successful: \"{englishPrompt}\"");
        }
        catch (Exception ex)
        {
            // Service 層からの例外をキャッチ
            Console.Error.WriteLine($"Error in /api/test-imagen endpoint calling GeminiService for translation: {ex.Message}");
            return Results.Problem($"プロンプトの翻訳中にエラーが発生しました。", statusCode: 500);
        }

        // --- 2. 翻訳されたプロンプトで Vertex AI Imagen API を呼び出し ---
        ImageGenerationResponse imageResponse;
        try
        {
            // ★ ImagenService のメソッドを呼び出すだけ！ ★
            Console.WriteLine($"Requesting image generation with prompt: \"{englishPrompt}\"...");
            imageResponse = await imagenService.GenerateImageAsync(englishPrompt);
            Console.WriteLine("Image generation successful!");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error during image generation: {ex.Message}");
            return Results.Problem("画像生成中にエラーが発生しました。", statusCode: 500);
        }

        // --- 3. レスポンスを返す ---
        return Results.Ok(imageResponse);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Unexpected error in /api/test-imagen: {ex.GetType().Name}: {ex.Message}");
        Console.WriteLine(ex.ToString()); // スタックトレースも出力（デバッグ用）
        return Results.Problem("画像生成リクエストの処理中に予期せぬエラーが発生しました。", statusCode: 500);
    }
}).WithName("TestImagen");

app.Run();
