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
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
// builder.Services.AddSwaggerGen(); // Swagger を使用する場合はコメント解除

var app = builder.Build();

// --- HTTPリクエストパイプラインの設定 ---
app.UseHttpsRedirection();
app.UseCors(MyAllowSpecificOrigins); // CORSミドルウェアを使用

// --- APIエンドポイントの定義 ---
app.MapControllers();

app.Run();
