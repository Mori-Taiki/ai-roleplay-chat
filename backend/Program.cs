using Google.Cloud.AIPlatform.V1;
using AiRoleplayChat.Backend.Services;
using Microsoft.EntityFrameworkCore;
using AiRoleplayChat.Backend.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using AiRoleplayChat.Backend.Adapters.Image;
using AiRoleplayChat.Backend.Adapters.Text;
using AiRoleplayChat.Backend.Application.Ports;
using AiRoleplayChat.Backend.Application.Prompts;
using AiRoleplayChat.Backend.Application.Routing;
using AiRoleplayChat.Backend.Options;

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

// --- Legacy services (deprecated in favor of hexagonal architecture) ---
// builder.Services.AddScoped<IGeminiService, GeminiService>();
builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();
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
// builder.Services.AddScoped<IImagenService, ImagenService>();
// builder.Services.AddScoped<IImagenService, ReplicateService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IChatMessageService, ChatMessageService>();
builder.Services.AddScoped<IChatSessionService, ChatSessionService>();
builder.Services.AddScoped<IApiKeyService, ApiKeyService>();
// Removed IUserSettingsService - now handled by UserAiSettings controller directly
builder.Services.AddScoped<IAiGenerationSettingsService, AiGenerationSettingsService>();

// --- Hexagonal Architecture Services ---
// Configure ProviderOptions
builder.Services.Configure<ProviderOptions>(
    builder.Configuration.GetSection(ProviderOptions.SectionName));

// Register Prompt Compiler
builder.Services.AddScoped<IPromptCompiler, PromptCompiler>();

// Register Adapters as Ports
builder.Services.AddScoped<ITextModelPort, GeminiTextAdapter>();
builder.Services.AddScoped<IImageModelPort, ReplicateImageAdapter>();

// Register Router
builder.Services.AddScoped<ILlmRouter, LlmRouter>();
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

var connectionString = builder.Configuration.GetConnectionString("SqliteConnection"); // 新しい接続文字列名

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString)
);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>(); // AppDbContext を取得
        context.Database.Migrate(); // マイグレーションを適用
        // 必要であれば、ここで初期データのシーディングなども行えます
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating or initializing the database.");
        // 本番環境では、ここでアプリケーションを停止するなどの処理も検討できます
        throw; // エラーを再スローして起動を失敗させるか、ログに記録して続行するかは要件によります
    }
}

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
