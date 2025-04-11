using Google.Cloud.AIPlatform.V1;
using AiRoleplayChat.Backend.Services; 
using Microsoft.EntityFrameworkCore;
using AiRoleplayChat.Backend.Data;

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

// User Secrets (または環境変数、appsettings.json) から接続文字列を取得
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found in configuration.");

// AppDbContext を DI コンテナに登録し、MySQL を使うように設定
builder.Services.AddDbContextPool<AppDbContext>(options => // または AddDbContext
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
            // オプション: 開発中に SQL ログや詳細エラーを見たい場合 (本番では注意)
            // .EnableSensitiveDataLogging()
            // .EnableDetailedErrors()
);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    Console.WriteLine("[DB Check] Attempting to connect to the database...");
    try
    {
        // DIコンテナから AppDbContext のスコープを作成してインスタンスを取得
        // (app.Services から直接取得するよりスコープを使う方が推奨される)
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // データベースに実際に接続できるかテスト
        var canConnect = await dbContext.Database.CanConnectAsync();

        if (canConnect)
        {
            // 接続成功！
            Console.ForegroundColor = ConsoleColor.Green; // 見やすいように色付け（任意）
            Console.WriteLine("[DB Check] Successfully connected to the database.");
            Console.ResetColor(); // 色を元に戻す
        }
        else
        {
            // 接続失敗 (CanConnectAsync が false を返した場合)
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine("[DB Check] Failed to connect to the database. CanConnectAsync returned false.");
            Console.Error.WriteLine("=> Check your connection string in User Secrets and Azure firewall settings.");
            Console.ResetColor();
            // ここで処理を続行するか、エラーとして停止するかは選択可能
        }
    }
    catch (Exception ex)
    {
        // 接続中に例外が発生した場合 (接続文字列の形式エラー、ネットワーク問題、認証エラーなど)
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine($"[DB Check] Error connecting to the database: {ex.Message}");
        // 例外の詳細を出力すると原因特定に役立つ場合がある
        // Console.Error.WriteLine(ex.ToString());
        Console.ResetColor();
    }
}

// --- HTTPリクエストパイプラインの設定 ---
app.UseHttpsRedirection();
app.UseCors(MyAllowSpecificOrigins); // CORSミドルウェアを使用

// --- APIエンドポイントの定義 ---
app.MapControllers();

app.Run();
