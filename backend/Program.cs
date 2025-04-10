using Google.Cloud.AIPlatform.V1;
using Google.Protobuf.WellKnownTypes;
using ProtoValue = Google.Protobuf.WellKnownTypes.Value; // Value に ProtoValue という別名を付ける
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


// Vertex AI クライアントを生成するヘルパーメソッド
PredictionServiceClient CreatePredictionServiceClient()
{
    
    // エンドポイントを指定 (例: us-central1)
    string location = "us-central1"; // ★ 必要なら変更: プロジェクトや利用モデルに合わせる (例: "asia-northeast1")
    string endpoint = $"{location}-aiplatform.googleapis.com";

    // GOOGLE_APPLICATION_CREDENTIALS 環境変数が設定されていれば、
    // Credential を明示的に指定しなくても、ライブラリが自動で認証情報を読み込みます。
    return new PredictionServiceClientBuilder
    {
        Endpoint = endpoint
        // Credential プロパティは指定不要！
    }.Build();
}

app.MapPost("/api/test-imagen", async (ImageGenRequest request, IHttpClientFactory clientFactory, IConfiguration config) =>
{
    try
    {
        string japanesePrompt = request.Prompt;
        if (string.IsNullOrWhiteSpace(japanesePrompt))
        {
            return Results.BadRequest(new { Message = "Prompt cannot be empty." });
        }

        // --- 1. Gemini API で日本語プロンプトを英語に翻訳 ---
        Console.WriteLine($"Translating Japanese prompt: \"{japanesePrompt}\" using Gemini API...");
        string englishPrompt = string.Empty; // 翻訳結果を格納する変数

        var apiKey = config["Gemini:ApiKey"]; // User Secrets または appsettings から取得
        if (string.IsNullOrEmpty(apiKey))
        {
            Console.Error.WriteLine("Gemini API Key is not configured.");
            return Results.Problem("Gemini API Key is not configured.", statusCode: 500);
        }
        // 翻訳に適したモデルを選ぶ (flashで十分か、必要ならproなど)
        var translationModel = "gemini-2.0-flash";
        var geminiApiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{translationModel}:generateContent?key={apiKey}";
        var httpClient = clientFactory.CreateClient();

        // 翻訳用の指示を作成 (より良い指示に調整可能)
        string translationInstruction = $"Translate the following Japanese text into a detailed English prompt suitable for an image generation AI (like Imagen). Focus on descriptive nouns and adjectives. Japanese text: \"{japanesePrompt}\"";

        // Gemini APIリクエストを作成 (既存のモデルクラスを再利用)
        var geminiRequest = new GeminiApiRequest
        {
            Contents = new[] { new GeminiContent { Parts = new[] { new GeminiPart { Text = translationInstruction } } } },
            // 翻訳タスク向けの設定 (例: Temperatureを低めに)
            GenerationConfig = new GenerationConfig { Temperature = 0.2, MaxOutputTokens = 256 } // トークン数は翻訳結果に合わせて調整
        };

        try // Gemini API呼び出し部分のエラーハンドリング
        {
            var geminiResponseRaw = await httpClient.PostAsJsonAsync(geminiApiUrl, geminiRequest);
            
            string rawJsonResponseBody = await geminiResponseRaw.Content.ReadAsStringAsync();
            Console.WriteLine($"[DEBUG] Raw Gemini API Response Body for Translation: {rawJsonResponseBody}");

            if (!geminiResponseRaw.IsSuccessStatusCode)
            {
                var errorContent = await geminiResponseRaw.Content.ReadAsStringAsync();
                Console.Error.WriteLine($"Gemini API Error (Translation): {geminiResponseRaw.StatusCode} - {errorContent}");
                // 翻訳失敗時は、エラーを返すか、あるいは日本語のまま進むか選択肢あり
                // ここではエラーを返すことにする
                return Results.Problem($"Failed to translate prompt via Gemini API: {geminiResponseRaw.StatusCode}", statusCode: 500);
            }

            var geminiResponse = await geminiResponseRaw.Content.ReadFromJsonAsync<GeminiApiResponse>();
            // 翻訳結果のテキストを取得 (nullチェックとTrimを追加)
            englishPrompt = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(englishPrompt))
            {
                 Console.Error.WriteLine("Gemini API returned empty translation for the prompt.");
                 // 翻訳結果が空の場合もエラーとする
                 return Results.Problem("Failed to get a valid translation from Gemini API.", statusCode: 500);
            }
             Console.WriteLine($"Translation successful: \"{englishPrompt}\"");
        }
        catch (Exception ex) // ネットワークエラーなどもキャッチ
        {
            Console.Error.WriteLine($"Error during Gemini API call for translation: {ex.Message}");
            return Results.Problem($"Failed to translate prompt: {ex.Message}", statusCode: 500);
        }
        // --- 翻訳処理ここまで ---


        // --- 2. 翻訳された英語プロンプトで Imagen API を呼び出す ---
        string projectId = "gen-lang-client-0605269968";
        string location = "asia-northeast1";     // ★ クライアント作成時と同じリージョンを指定
        // imagegeneration@006 モデル (2025/04時点) - 最新版はドキュメントで確認
        string modelId = "imagegeneration@006"; // ★ 必要なら最新モデルIDに変更

        // エンドポイント名の組み立て
        var endpointName = EndpointName.FromProjectLocationPublisherModel(projectId, location, "google", modelId);

        // PredictionServiceClient の作成 (環境変数が使われる)
        var predictionServiceClient = CreatePredictionServiceClient();

        var instances = new List<ProtoValue>
        {
            ProtoValue.ForStruct(new Struct
            {
                Fields =
                {
                    { "prompt", ProtoValue.ForString(englishPrompt) },
                    { "sampleCount", ProtoValue.ForNumber(1) } // 生成する画像の枚数
                    // 必要に応じて他のパラメータ (negativePrompt, aspectRatio など) を追加
                    // 例: { "aspectRatio", Value.ForString("1:1") } // 正方形
                }
            })
        };

        // API呼び出し
        Console.WriteLine($"Calling Vertex AI Imagen API (Project: {projectId}, Location: {location}, Model: {modelId})...");
        PredictResponse imagenResponse = await predictionServiceClient.PredictAsync(endpointName, instances, null); // parameters は null で試す

        Console.WriteLine("API call successful!");

        // ▼▼▼ レスポンスから画像データを抽出して返す処理 ▼▼▼
        if (imagenResponse.Predictions.Count > 0)
        {
            var firstPrediction = imagenResponse.Predictions[0]; // 最初の予測結果を取得

            // 必要なフィールドが存在し、かつ正しい型かを確認しながら安全にアクセス
            if (firstPrediction.KindCase == ProtoValue.KindOneofCase.StructValue &&
                firstPrediction.StructValue.Fields.TryGetValue("bytesBase64Encoded", out var base64Value) &&
                base64Value.KindCase == ProtoValue.KindOneofCase.StringValue &&
                firstPrediction.StructValue.Fields.TryGetValue("mimeType", out var mimeTypeValue) &&
                mimeTypeValue.KindCase == ProtoValue.KindOneofCase.StringValue)
            {
                // Base64データとMIMEタイプを取得
                string base64Data = base64Value.StringValue;
                string mimeType = mimeTypeValue.StringValue;

                Console.WriteLine($"Image data received: MimeType={mimeType}, Base64Data Length={base64Data.Length}");

                // 定義したレコードを使ってレスポンスを作成
                var imageResponse = new ImageGenerationResponse(mimeType, base64Data);
                return Results.Ok(imageResponse); // JSONとして返す
            }
            else
            {
                // 必要なフィールドが見つからない、または型が違う場合
                Console.Error.WriteLine("Error: Could not find expected fields 'bytesBase64Encoded' (String) or 'mimeType' (String) in the prediction response struct.");
                Console.Error.WriteLine($"Actual Prediction[0] structure: {firstPrediction}"); // デバッグ用に構造を出力
                return Results.Problem("API response did not contain expected image data structure.");
            }
        }
        else
        {
            // 予測結果が空の場合
            Console.WriteLine("API response contained no predictions.");
            return Results.Problem("API response contained no predictions.");
        }
        // ▲▲▲ レスポンス処理ここまで ▲▲▲
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error calling Vertex AI Imagen API: {ex.Message}");
        // エラーの詳細を出力 (デバッグ用)
        Console.WriteLine(ex.ToString());
        // エラーメッセージを返す
        return Results.Problem($"Error calling Imagen API: {ex.Message}");
    }
}).WithName("TestImagen"); // エンドポイントに名前を付ける (任意)

app.Run();


// Gemini APIとのリクエスト/レスポンス用レコード定義
public record ChatRequest(string Prompt);
public record ChatResponse(string Reply);
// Imagen APIのリクエスト/レスポンス用レコード定義
public record ImageGenRequest(string Prompt);
public record ImageGenerationResponse(string MimeType, string Base64Data);


// --- Gemini API 用のリクエスト/レスポンスモデル定義 ---

// Gemini API リクエストボディ用
public class GeminiApiRequest
{
    [JsonPropertyName("contents")]
    public required GeminiContent[] Contents { get; set; }
    
    [JsonPropertyName("generationConfig")] 
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
