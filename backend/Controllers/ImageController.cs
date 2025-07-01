using AiRoleplayChat.Backend.Data;
using AiRoleplayChat.Backend.Models;
using AiRoleplayChat.Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AiRoleplayChat.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ImageController : BaseApiController // ★ ControllerBaseからBaseApiControllerに変更
{
    // ★ 必要なサービスをDIで受け取るように変更
    private readonly IGeminiService _geminiService;
    private readonly IImagenService _imagenService;
    private readonly IBlobStorageService _blobStorageService;
    private readonly AppDbContext _context;

    // ★ コンストラクタを大幅に変更
    public ImageController(
        IUserService userService, // BaseApiControllerに必要
        IGeminiService geminiService,
        IImagenService imagenService,
        IBlobStorageService blobStorageService,
        AppDbContext context,
        ILogger<ImageController> logger)
        : base(userService, logger) // BaseApiControllerのコンストラクタを呼び出し
    {
        _geminiService = geminiService;
        _imagenService = imagenService;
        _blobStorageService = blobStorageService;
        _context = context;
    }

    /// <summary>
    /// 指定されたメッセージIDに基づいて画像を生成し、Blobにアップロード後、そのURLを返します。
    /// </summary>
    // ★ 既存の `GenerateImage` メソッドを置き換える新しいメソッド
    [HttpPost("generate-and-upload")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GenerateAndUploadImage([FromBody] ImageGenRequest request, CancellationToken cancellationToken)
    {
        var (appUserId, errorResult) = await GetCurrentAppUserIdAsync(cancellationToken);
        if (errorResult != null) return errorResult;

        _logger.LogInformation("Image generation request received for MessageId: {MessageId} from UserId: {UserId}", request.MessageId, appUserId);

        if (request.MessageId <= 0)
        {
            return BadRequest(new { Message = "有効なメッセージIDは必須です。" });
        }

        // 1. 対象のメッセージと、それが属するセッションIDをDBから取得 (追跡なし)
        var targetMessage = await _context.ChatMessages.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == request.MessageId && m.UserId == appUserId, cancellationToken);

        if (targetMessage == null)
        {
            _logger.LogWarning("User {UserId} attempted to generate an image for a non-existent or unauthorized message {MessageId}", appUserId, request.MessageId);
            return Forbid("指定されたメッセージを操作する権限がありません。");
        }

        // 2. DBから会話履歴を取得
        var history = await _context.ChatMessages.AsNoTracking()
                            .Where(m => m.SessionId == targetMessage.SessionId && m.Timestamp <= targetMessage.Timestamp)
                            .OrderBy(m => m.Timestamp)
                            .ToListAsync(cancellationToken);

        if (!history.Any())
        {
            return Problem("画像生成の元となる会話履歴が見つかりませんでした。");
        }

        // 3. Geminiにプロンプト生成を依頼
        string englishPrompt;
        try
        {
            englishPrompt = await _geminiService.GenerateImagePromptAsync(history, cancellationToken);
            _logger.LogInformation("Generated image prompt for MessageId {MessageId}: '{Prompt}'", request.MessageId, englishPrompt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate image prompt for MessageId {MessageId}", request.MessageId);
            return Problem("AIによる画像プロンプトの生成に失敗しました。");
        }

        // 4. 画像生成サービスを呼び出し
        var generationResult = await _imagenService.GenerateImageAsync(englishPrompt, cancellationToken);
        if (generationResult is not { ImageBytes: not null, MimeType: not null })
        {
            return Problem("画像データの生成に失敗しました。");
        }
        var (imageBytes, mimeType) = generationResult.Value;

        // 5. Blob Storageへアップロード
        string blobName = $"{Guid.NewGuid()}.png"; // 一意のファイル名
        string imageUrl;
        try
        {
            using var imageStream = new MemoryStream(imageBytes);
            imageUrl = await _blobStorageService.UploadAsync(imageStream, blobName, mimeType, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload image to Blob Storage for MessageId {MessageId}", request.MessageId);
            return Problem("画像のアップロード処理中にエラーが発生しました。");
        }

        // 6. データベースのメッセージレコードを更新 (追跡している新しいインスタンスで)
        var messageToUpdate = await _context.ChatMessages.FindAsync(new object[] { request.MessageId }, cancellationToken);
        if (messageToUpdate != null)
        {
            messageToUpdate.ImageUrl = imageUrl;
            messageToUpdate.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Successfully updated MessageId {MessageId} with ImageUrl: {ImageUrl}", request.MessageId, imageUrl);
        }
        else
        {
            _logger.LogWarning("Could not find message {MessageId} to update ImageUrl, but image was uploaded to {ImageUrl}", request.MessageId, imageUrl);
        }

        // 7. フロントエンドに画像のURLを返す
        return Ok(new { ImageUrl = imageUrl });
    }
}