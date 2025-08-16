using AiRoleplayChat.Backend.Data;
using AiRoleplayChat.Backend.Models;
using AiRoleplayChat.Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AiRoleplayChat.Backend.Application.Routing;
using AiRoleplayChat.Backend.Application.Contracts;
using AiRoleplayChat.Backend.Domain.Entities;

namespace AiRoleplayChat.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ImageController : BaseApiController // ★ ControllerBaseからBaseApiControllerに変更
{
    // ★ 必要なサービスをDIで受け取るように変更
    private readonly ILlmRouter _llmRouter;
    private readonly IBlobStorageService _blobStorageService;
    private readonly IChatMessageService _chatMessageService;
    private readonly AppDbContext _context;

    // ★ コンストラクタを大幅に変更
    public ImageController(
        IUserService userService, // BaseApiControllerに必要
        ILlmRouter llmRouter,
        IBlobStorageService blobStorageService,
        IChatMessageService chatMessageService,
        AppDbContext context,
        ILogger<ImageController> logger)
        : base(userService, logger) // BaseApiControllerのコンストラクタを呼び出し
    {
        _llmRouter = llmRouter;
        _blobStorageService = blobStorageService;
        _chatMessageService = chatMessageService;
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

        // 2. DBからキャラクター設定と会話履歴を取得
        var character = await _context.CharacterProfiles.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == targetMessage.CharacterProfileId, cancellationToken);

        if (character == null)
        {
            return Problem("画像生成の対象となるキャラクターが見つかりませんでした。");
        }

        var history = await _context.ChatMessages.AsNoTracking()
                            .Where(m => m.SessionId == targetMessage.SessionId && m.Timestamp <= targetMessage.Timestamp)
                            .OrderBy(m => m.Timestamp)
                            .ToListAsync(cancellationToken);

        if (!history.Any())
        {
            return Problem("画像生成の元となる会話履歴が見つかりませんでした。");
        }

        if (!history.Any())
        {
            return Problem("画像生成の元となる会話履歴が見つかりませんでした。");
        }

        // 3. Generate English prompt using text model through hexagonal architecture
        string englishPrompt;
        try
        {
            // Convert chat history to ChatTurn format for text model
            var chatHistory = ConvertToChatTurns(history);
            
            // Resolve text model for prompt generation
            var textModel = await _llmRouter.ResolveTextModelAsync(character, appUserId);
            
            // Create image prompt instruction
            var imagePromptInstruction = BuildImagePromptInstruction(character);
            
            // Create text request for image prompt generation
            var promptRequest = new TextRequest(
                Prompt: "Generate image description for current conversation",
                SystemPrompt: imagePromptInstruction,
                History: chatHistory.TakeLast(6).ToList(), // Limit to recent history
                MaxTokens: 2048,
                Temperature: 0.4,
                UserId: appUserId
            );
            
            var promptCompletion = await textModel.GenerateTextAsync(promptRequest, cancellationToken);
            englishPrompt = promptCompletion.Text.Trim();
            
            _logger.LogInformation("Generated image prompt for MessageId {MessageId}: '{Prompt}'", request.MessageId, englishPrompt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate image prompt for MessageId {MessageId}", request.MessageId);
            return Problem("AIによる画像プロンプトの生成に失敗しました。");
        }

        // 4. Generate image using image model through hexagonal architecture
        byte[]? imageBytes = null;
        string? mimeType = null;
        string? actualPrompt = null;
        string? modelId = null;
        string? serviceName = null;
        
        try
        {
            // Resolve image model
            var imageModel = await _llmRouter.ResolveImageModelAsync(character, appUserId);
            
            // Create image request
            var imageRequest = new ImageRequest(
                Prompt: englishPrompt,
                UserId: appUserId
            );
            
            // Generate image
            var imageResult = await imageModel.GenerateImageAsync(imageRequest, cancellationToken);
            
            if (imageResult != null)
            {
                imageBytes = imageResult.ImageBytes;
                mimeType = imageResult.MimeType;
                actualPrompt = imageResult.ActualPrompt;
                modelId = imageResult.ModelId;
                serviceName = imageResult.ServiceName;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate image for MessageId {MessageId}", request.MessageId);
            return Problem("画像生成に失敗しました。");
        }
        
        if (imageBytes == null || mimeType == null)
        {
            return Problem("画像データの生成に失敗しました。");
        }

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
            messageToUpdate.ImagePrompt = actualPrompt;
            messageToUpdate.ModelId = modelId;
            messageToUpdate.ServiceName = serviceName;
            messageToUpdate.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Successfully updated MessageId {MessageId} with ImageUrl: {ImageUrl}, ModelId: {ModelId}, ServiceName: {ServiceName}", 
                request.MessageId, imageUrl, modelId, serviceName);
        }
        else
        {
            _logger.LogWarning("Could not find message {MessageId} to update ImageUrl, but image was uploaded to {ImageUrl}", request.MessageId, imageUrl);
        }

        // 7. フロントエンドに画像のURLを返す
        return Ok(new { ImageUrl = imageUrl });
    }

    /// <summary>
    /// キャラクターの画像ギャラリーを取得します。
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ImageGalleryResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetImages(
        [FromQuery] int characterId,
        [FromQuery] string? sessionId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 40,
        CancellationToken cancellationToken = default)
    {
        var (appUserId, errorResult) = await GetCurrentAppUserIdAsync(cancellationToken);
        if (errorResult != null) return errorResult;

        if (characterId <= 0 || page <= 0 || pageSize <= 0 || pageSize > 100)
        {
            return BadRequest(new { Message = "無効なクエリパラメータです。" });
        }

        try
        {
            var (items, total) = await _chatMessageService.GetImageMessagesAsync(
                appUserId!.Value, characterId, sessionId, page, pageSize, cancellationToken);

            var imageDtos = items.Select(m => new ImageItemDto
            {
                MessageId = m.Id,
                CharacterId = m.CharacterProfileId,
                SessionId = m.SessionId,
                SessionTitle = m.Session?.Id, // Simple session ID as title for now
                ImageUrl = m.ImageUrl!,
                ImagePrompt = m.ImagePrompt,
                ModelId = m.ModelId,
                ServiceName = m.ServiceName,
                CreatedAt = m.CreatedAt
            }).ToList();

            var response = new ImageGalleryResponseDto
            {
                Items = imageDtos,
                Total = total,
                Page = page,
                PageSize = pageSize
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving images for CharacterId: {CharacterId}, UserId: {UserId}", characterId, appUserId);
            return StatusCode(500, "画像一覧の取得中にエラーが発生しました。");
        }
    }

    /// <summary>
    /// 画像メッセージをソフトデリートします。
    /// </summary>
    [HttpDelete("{messageId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteImage(int messageId, CancellationToken cancellationToken = default)
    {
        var (appUserId, errorResult) = await GetCurrentAppUserIdAsync(cancellationToken);
        if (errorResult != null) return errorResult;

        if (messageId <= 0)
        {
            return BadRequest(new { Message = "無効なメッセージIDです。" });
        }

        try
        {
            var success = await _chatMessageService.SoftDeleteImageMessageAsync(
                appUserId!.Value, messageId, cancellationToken);

            if (!success)
            {
                return NotFound(new { Message = "画像メッセージが見つからない、または削除権限がありません。" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting image MessageId: {MessageId}, UserId: {UserId}", messageId, appUserId);
            return StatusCode(500, "画像削除中にエラーが発生しました。");
        }
    }

    /// <summary>
    /// Convert chat message history to ChatTurn format for hexagonal architecture
    /// </summary>
    private List<ChatTurn> ConvertToChatTurns(List<ChatMessage> history)
    {
        var turns = new List<ChatTurn>();
        
        for (int i = 0; i < history.Count; i += 2)
        {
            var userMessage = history[i];
            var aiMessage = i + 1 < history.Count ? history[i + 1] : null;
            
            // Only process if this is a user message
            if (userMessage.Sender == "user")
            {
                turns.Add(new ChatTurn(
                    UserMessage: userMessage.Text,
                    AssistantMessage: aiMessage?.Sender == "ai" ? aiMessage.Text : null,
                    Timestamp: userMessage.Timestamp
                ));
            }
        }
        
        return turns;
    }

    /// <summary>
    /// Build image prompt instruction with character information
    /// </summary>
    private string BuildImagePromptInstruction(CharacterProfile character)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("## Image Generation Instruction");
        sb.AppendLine("Based on the conversation context, generate a detailed English image description that captures the current scene, action, or emotion. Focus on visual details that would help create an artistic illustration.");
        sb.AppendLine();
        sb.AppendLine("## Character Profile:");
        sb.Append("- Name: ").AppendLine(character.Name);
        if (!string.IsNullOrEmpty(character.Personality))
            sb.Append("- Personality: ").AppendLine(character.Personality);
        if (!string.IsNullOrEmpty(character.Appearance))
            sb.Append("- Appearance: ").AppendLine(character.Appearance);
        if (!string.IsNullOrEmpty(character.Backstory))
            sb.Append("- Backstory: ").AppendLine(character.Backstory);
        sb.AppendLine();
        sb.AppendLine("Generate a concise but vivid English description suitable for image generation (max 100 words).");
        
        return sb.ToString();
    }
}