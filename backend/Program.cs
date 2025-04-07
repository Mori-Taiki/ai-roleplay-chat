using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers; // Headersのため追加
using System.Text.Json; // JsonSerializerのため追加 (必要に応じて)
using System.Text.Json.Serialization; // JsonPropertyNameのため追加

// CORSポリシー名を定義 
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

var builder = WebApplication.CreateBuilder(args);

// --- DIコンテナへのサービス登録 ---

// CORS 
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins, policy =>
    {
        policy.WithOrigins("http://localhost:5173") // React開発サーバーのオリジン
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// ★★★ HttpClientサービスを登録 ★★★
builder.Services.AddHttpClient();

// JSONオプション設定 (前回追加したもの)
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
});

builder.Services.AddEndpointsApiExplorer();
// builder.Services.AddSwaggerGen(); // Swaggerを使う場合はコメント解除

var app = builder.Build();

// --- HTTPリクエストパイプラインの設定 ---

// if (app.Environment.IsDevelopment()) // Swaggerを使う場合はコメント解除
// {
//     app.UseSwagger();
//     app.UseSwaggerUI();
// }

app.UseHttpsRedirection();
app.UseCors(MyAllowSpecificOrigins); // CORSミドルウェアを使用 


// --- APIエンドポイントの定義 ---

app.MapPost("/api/chat", async (ChatRequest request, IHttpClientFactory clientFactory, IConfiguration config) => // ★ async と DI を追加
{
    Console.WriteLine($"Received prompt: {request.Prompt}");

    // --- Gemini API 呼び出し処理 ---
    var apiKey = config["Gemini:ApiKey"]; // appsettings.Development.json からキーを取得
    if (string.IsNullOrEmpty(apiKey))
    {
        Console.Error.WriteLine("Gemini API Key is not configured in appsettings.Development.json");
        return Results.Problem("APIキーが設定されていません。", statusCode: 500);
    }

    var model = "gemini-1.5-flash-latest"; // 使用するモデル
    var geminiApiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

    // HttpClient を IHttpClientFactory から取得
    var httpClient = clientFactory.CreateClient();

    // Gemini API へのリクエストボディを作成
    var geminiRequest = new GeminiApiRequest
    {
        Contents = new[] {
            new GeminiContent {
                Parts = new[] {
                    new GeminiPart { Text = request.Prompt }
                }
            }
        },
        // 必要に応じて GenerationConfig などを追加
        GenerationConfig = new GenerationConfig { Temperature = 0.7, MaxOutputTokens = 100 }
    };

    try
    {
        // Gemini API へ POST リクエストを送信
        // System.Net.Http.Json の PostAsJsonAsync を使うと便利
        var response = await httpClient.PostAsJsonAsync(geminiApiUrl, geminiRequest);

        if (!response.IsSuccessStatusCode)
        {
            // Gemini API からエラー応答が返ってきた場合
            var errorContent = await response.Content.ReadAsStringAsync();
            Console.Error.WriteLine($"Gemini API Error: {response.StatusCode} - {errorContent}");
            return Results.Problem($"Gemini APIとの通信に失敗しました: {response.StatusCode}", statusCode: (int)response.StatusCode);
        }

        // 成功応答をデシリアライズ
        var geminiResponse = await response.Content.ReadFromJsonAsync<GeminiApiResponse>();

        // 応答テキストを取得 (nullチェックを含む)
        var replyText = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text ?? "すみません、応答を取得できませんでした。";

        var chatResponse = new ChatResponse(replyText);
        return Results.Ok(chatResponse);

    }
    catch (Exception ex)
    {
        // ネットワークエラーなど、その他の例外処理
        Console.Error.WriteLine($"Exception during Gemini API call: {ex.Message}");
        return Results.Problem($"API呼び出し中にエラーが発生しました: {ex.Message}", statusCode: 500);
    }
    // --- Gemini API 呼び出しここまで ---

})
.WithName("PostChatMessage");
// .WithOpenApi(); // Swaggerを使う場合はコメント解除

app.Run();


// --- フロントエンドとのリクエスト/レスポンス用のレコード定義  ---
public record ChatRequest(string Prompt);
public record ChatResponse(string Reply);


// --- Gemini API 用のリクエスト/レスポンスモデル定義 ---

// Gemini API リクエストボディ用
public class GeminiApiRequest
{
    [JsonPropertyName("contents")]
    public required GeminiContent[] Contents { get; set; }

    public required GenerationConfig GenerationConfig { get; set; }
}

public class GeminiContent
{
    [JsonPropertyName("parts")]
    public required GeminiPart[] Parts { get; set; }
    // 必要なら追加: public string Role { get; set; } // "user" or "model"
}

public class GeminiPart
{
    [JsonPropertyName("text")]
    public required string Text { get; set; }
}

// 必要なら GenerationConfig を定義
public class GenerationConfig
{
    [JsonPropertyName("temperature")]
    public double Temperature { get; set; } = 0.7; // 例: デフォルト値

    [JsonPropertyName("maxOutputTokens")]
    public int MaxOutputTokens { get; set; } = 1024; // 例: デフォルト値
    // 他にも topP, topK など設定可能
}



// Gemini API レスポンスボディ用 (必要な部分のみ)
public class GeminiApiResponse
{
    [JsonPropertyName("candidates")]
    public required GeminiCandidate[] Candidates { get; set; }
    // 必要なら追加: public PromptFeedback PromptFeedback { get; set; }
}

public class GeminiCandidate
{
    [JsonPropertyName("content")]
    public required GeminiContent Content { get; set; }

    [JsonPropertyName("finishReason")]
    public required string FinishReason { get; set; }

    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("safetyRatings")]
    public SafetyRating[]? SafetyRatings { get; set; }
}

public class SafetyRating
{
    [JsonPropertyName("category")]
    public required string Category { get; set; }

    [JsonPropertyName("probability")]
    public required string Probability { get; set; } // 例: "NEGLIGIBLE"
}

/* // 必要なら PromptFeedback を定義
public class PromptFeedback
{
    [JsonPropertyName("safetyRatings")]
    public SafetyRating[] SafetyRatings { get; set; }
}
*/
