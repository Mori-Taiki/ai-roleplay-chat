using Google.Cloud.AIPlatform.V1;
using AiRoleplayChat.Backend.Services;
using Microsoft.EntityFrameworkCore;
using AiRoleplayChat.Backend.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;

// CORSポリシー名を定義
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

var builder = WebApplication.CreateBuilder(args);

var frontendUrl = builder.Configuration["FrontendUrl"] ?? "http://localhost:5173";
// --- サービス登録 ---
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins, policy =>
    {
        policy.WithOrigins(frontendUrl) // フロントエンドの開発サーバー
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
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IChatMessageService, ChatMessageService>();
builder.Services.AddScoped<IChatSessionService, ChatSessionService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // ★ Swagger で認証を使えるようにする設定 (任意だが推奨)
    options.AddSecurityDefinition("oauth2", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.OAuth2,
        Flows = new Microsoft.OpenApi.Models.OpenApiOAuthFlows
        {
            // B2C の Authorization Code フロー (Implicit フローも使えるが非推奨)
            AuthorizationCode = new Microsoft.OpenApi.Models.OpenApiOAuthFlow
            {
                // B2C の Authorize エンドポイント (テナント名とポリシー名を反映)
                AuthorizationUrl = new Uri($"https://{builder.Configuration["AzureAdB2C:Domain"]}/{builder.Configuration["AzureAdB2C:SignUpSignInPolicyId"]}/oauth2/v2.0/authorize"),
                // B2C の Token エンドポイント
                TokenUrl = new Uri($"https://{builder.Configuration["AzureAdB2C:Domain"]}/{builder.Configuration["AzureAdB2C:SignUpSignInPolicyId"]}/oauth2/v2.0/token"),
                Scopes = new Dictionary<string, string>
                {
                    // ★ API で公開したスコープを定義 (例: api://{APIのClientId}/API.Access)
                    // スコープの完全な URI を設定ファイルから取得するのが望ましい
                    // "api://{your-backend-api-client-id}/API.Access": "Access the chat API"
                    [builder.Configuration["AzureAdB2C:ApiScopeUrl"] ?? $"api://{builder.Configuration["AzureAdB2C:ClientId"]}/API.Access"] = "APIへのアクセス"
                }
            }
        }
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "oauth2" }
            },
            new[] { builder.Configuration["AzureAdB2C:ApiScopeUrl"] ?? $"api://{builder.Configuration["AzureAdB2C:ClientId"]}/API.Access" } // ここもスコープURI
        }
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAdB2C"));

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
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        // ★ Swagger UI で OAuth2 認証を使えるようにする設定
        options.OAuthClientId(builder.Configuration["AzureAdB2C:ClientId"]); // APIのクライアントID
        // options.OAuthClientSecret("YOUR_CLIENT_SECRET"); // 通常SPA連携では不要
        options.OAuthAppName("Swagger UI for API");
        options.OAuthUsePkce(); // PKCE を使用
    });
}

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

// ★ 認証ミドルウェアを追加 
app.UseAuthentication();

// ★ 認可ミドルウェア 
app.UseAuthorization();

// --- APIエンドポイントの定義 ---
app.MapControllers();

app.Run();
