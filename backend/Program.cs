using Microsoft.AspNetCore.Mvc; // For [FromBody] potentially, though often inferred
// TODO:

// CORSポリシー名を定義
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

var builder = WebApplication.CreateBuilder(args);

// CORSサービスをDIコンテナに追加
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      policy =>
                      {
                          // 開発中はReact開発サーバーのオリジンを許可
                          // TODO: 本番環境ではより厳密なオリジンを指定する
                          policy.WithOrigins("http://localhost:5173") // ポート番号は自身の環境に合わせる
                                .AllowAnyHeader()
                                .AllowAnyMethod();
                          // もし動作しない場合は、一時的に .AllowAnyOrigin() を試すことも検討
                          // policy.AllowAnyOrigin()
                          //       .AllowAnyHeader()
                          //       .AllowAnyMethod();
                      });
});


// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
// デフォルトで追加されている場合があるSwagger関連のサービス (なければ不要)
builder.Services.AddEndpointsApiExplorer();
// builder.Services.AddSwaggerGen();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // デフォルトで追加されている場合があるSwagger UI (なければ不要)
    // app.UseSwagger();
    // app.UseSwaggerUI();
}

// HTTPSリダイレクションを有効にする (デフォルトで有効)
app.UseHttpsRedirection();

// ★★★ CORSミドルウェアを使用する ★★★
// app.Map... より前に記述することが重要
app.UseCors(MyAllowSpecificOrigins);


// --- APIエンドポイントの定義 ---

// デフォルトのHello Worldエンドポイントは削除またはコメントアウト
// app.MapGet("/", () => "Hello World!");

// 新しいチャットAPIエンドポイントを定義
app.MapPost("/api/chat", (ChatRequest request) =>
{
    Console.WriteLine($"Received prompt: {request.Prompt}"); // サーバーのコンソールにログ出力

    // TODO: ここで将来的にGemini APIを呼び出す処理を入れる

    // 今はダミーの応答を返す
    var reply = $"サーバーより: 「{request.Prompt}」を受け取りました。(これはダミー応答です)";
    var response = new ChatResponse(reply);

    // 結果をJSONで返す (ASP.NET Coreが自動でJSONに変換してくれる)
    return Results.Ok(response);
})
.WithName("PostChatMessage"); // エンドポイントに名前を付ける (任意)
// .WithOpenApi(); // Swagger/OpenAPIのドキュメントに表示する (任意)


app.Run();


// --- リクエスト/レスポンス用のレコード定義 ---
// ファイルの末尾や、別のファイルに定義してもOK

/// <summary>
/// フロントエンドから受け取るリクエストの型
/// </summary>
/// <param name="Prompt">ユーザーが入力したプロンプト</param>
public record ChatRequest(string Prompt);

/// <summary>
/// フロントエンドへ返すレスポンスの型
/// </summary>
/// <param name="Reply">AI(またはバックエンド)からの返信</param>
public record ChatResponse(string Reply);
