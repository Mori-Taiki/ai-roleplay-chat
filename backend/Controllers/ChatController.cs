using AiRoleplayChat.Backend.Models;
using AiRoleplayChat.Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using AiRoleplayChat.Backend.Domain.Entities;
using AiRoleplayChat.Backend.Data;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using System.Text.RegularExpressions;
using static AiRoleplayChat.Backend.Utils.PromptUtils;

namespace AiRoleplayChat.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatController : BaseApiController
{
    private readonly IGeminiService _geminiService;
    private readonly IImagenService _imagenService;
    private readonly AppDbContext _context;
    private readonly IChatMessageService _chatMessageService;

    public ChatController(
        AppDbContext context,
        IUserService userService,
        IGeminiService geminiService,
        IImagenService imagenService,
        IChatMessageService chatMessageService,
        ILogger<ChatController> logger)
    : base(userService, logger)
    {
        _geminiService = geminiService;
        _imagenService = imagenService;
        _context = context;
        _chatMessageService = chatMessageService;
    }

    [HttpGet("sessions/latest", Name = "GetLatestActiveSession")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<string>> GetLatestActiveSession(
    [FromQuery][Required] int characterId,
    CancellationToken cancellationToken)
    {
        var (appUserId, errorResult) = await GetCurrentAppUserIdAsync(cancellationToken);
        if (errorResult != null) return errorResult;

        var latestSession = await _context.ChatSessions
            .Where(s => s.CharacterProfileId == characterId && s.UserId == appUserId && s.EndTime == null) // アクティブなセッションを検索
            .OrderByDescending(s => s.StartTime) // 開始時刻が最新のもの
            .FirstOrDefaultAsync(cancellationToken);

        if (latestSession == null)
        {
            _logger.LogInformation("No active session found for Character {CharacterId} and User {UserId}", characterId, appUserId);
            return NotFound("アクティブなチャットセッションが見つかりません。");
        }

        _logger.LogInformation("Found latest active session: {SessionId}", latestSession.Id);
        return Ok(latestSession.Id);
    }

    [HttpGet("history", Name = "GetChatHistory")]
    [ProducesResponseType(typeof(IEnumerable<ChatMessageResponseDto>), StatusCodes.Status200OK)] // ★ DTO を使うことを推奨
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)] // セッションが見つからない場合
    public async Task<ActionResult<IEnumerable<ChatMessageResponseDto>>> GetChatHistory(
        [FromQuery][Required] string sessionId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received request to get chat history for Session {SessionId}", sessionId);
        var (appUserId, errorResult) = await GetCurrentAppUserIdAsync(cancellationToken);
        if (errorResult != null) return errorResult;

        // 0. セッションが存在するか確認
        var sessionExists = await _context.ChatSessions.AnyAsync(s => s.Id == sessionId && s.UserId == appUserId, cancellationToken);
        if (!sessionExists)
        {
            _logger.LogWarning("Session not found: {SessionId}", sessionId);
            return NotFound($"Session with ID {sessionId} not found.");
        }

        // 1. 指定された sessionId と UserId に紐づくメッセージを Timestamp の昇順で取得
        var messages = await _context.ChatMessages
                                     .Where(m => m.SessionId == sessionId && m.UserId == appUserId)
                                     .OrderBy(m => m.Timestamp) // 古い順に取得
                                                                // .Take(100) // 必要なら取得件数に上限を設ける
                                     .Select(m => new ChatMessageResponseDto // ★ エンティティを直接返さず DTO にマッピング推奨
                                     {
                                         Id = m.Id,
                                         Sender = m.Sender,
                                         Text = m.Text,
                                         ImageUrl = m.ImageUrl,
                                         Timestamp = m.Timestamp // ISO 8601 形式の文字列で返すのが一般的
                                     })
                                     .ToListAsync(cancellationToken);

        _logger.LogInformation("Returning {Count} messages for session {SessionId}", messages.Count, sessionId);
        return Ok(messages);
    }


    // POST /api/chat
    [HttpPost(Name = "PostChatMessage")]
    [ProducesResponseType(typeof(ChatResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChatResponse>> PostChatMessage([FromBody] ChatRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received chat request for Character {CharacterId}, Session {SessionId}", request.CharacterProfileId, request.SessionId ?? "New");

        var (appUserId, errorResult) = await GetCurrentAppUserIdAsync(cancellationToken);
        if (errorResult != null) return errorResult;
        if (appUserId == null) return BadRequest("User ID cannot be null.");

        // 0. キャラクターが存在するか確認
        var character = await _context.CharacterProfiles.FindAsync(new object[] { request.CharacterProfileId }, cancellationToken);
        if (character == null)
        {
            _logger.LogWarning("Character not found: {CharacterId}", request.CharacterProfileId);
            return NotFound($"Character with ID {request.CharacterProfileId} not found.");
        }

        // 1. セッションを特定または新規作成
        ChatSession? session = await GetOrCreateSessionAsync(request.SessionId, request.CharacterProfileId, appUserId.Value, cancellationToken);
        if (session == null) return StatusCode(500, "Failed to get or create session.");

        // 2. ユーザーの発言を保存
        ChatMessage userMessage; // 保存後のエンティティを受け取る
        try
        {
            userMessage = await _chatMessageService.AddMessageAsync(
                session.Id,
                request.CharacterProfileId,
                appUserId.Value,
                "user",
                request.Prompt,
                null, // imageUrl は null とする
                DateTime.UtcNow, // Timestamp
                cancellationToken
            );
            _logger.LogInformation("User message saved via service. SessionId: {SessionId}, MessageId: {MessageId}", session.Id, userMessage.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save user message using ChatMessageService for SessionId: {SessionId}", session.Id);
            return StatusCode(StatusCodes.Status500InternalServerError, "ユーザーメッセージの保存中にエラーが発生しました。");
        }


        // 3. 履歴を取得
        List<ChatMessage> history = await _context.ChatMessages
                                .Where(m => m.SessionId == session.Id)
                                .OrderByDescending(m => m.Timestamp) // 新しい順に取得
                                .Take(100)
                                .ToListAsync(cancellationToken);

        // 4. GeminiService で応答を取得
        string aiReplyTextWithPotentialTag;
        try
        {
            aiReplyTextWithPotentialTag = await _geminiService.GenerateChatResponseAsync(
                request.Prompt,
                SystemPromptHelper.AppendImageInstruction(character.SystemPrompt
                // システムプロンプトが無い場合は、キャラクタープロフィールから生成
                 ?? SystemPromptHelper.GenerateDefaultPrompt(character.Name, character.Personality, character.Tone, character.Backstory)),
                history,
                cancellationToken
             );
            _logger.LogInformation("Gemini response received for session {SessionId}", session.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Gemini service for session {SessionId}", session.Id);
            return StatusCode(StatusCodes.Status500InternalServerError, "AI応答の生成中にエラーが発生しました。");
        }

        string finalAiReplyText = aiReplyTextWithPotentialTag; // 最終的な応答テキスト
        string? generatedImageUrl = null;                     // 生成された画像URL
        Match imageTagMatch = Regex.Match(aiReplyTextWithPotentialTag, @"\[generate_image:\s*(.*?)\]");

        if (imageTagMatch.Success)
        {
            string imagePrompt = imageTagMatch.Groups[1].Value.Trim(); // タグから英語プロンプトを抽出
            _logger.LogInformation("Image generation requested by AI for session {SessionId}. Prompt: '{ImagePrompt}'", session.Id, imagePrompt);

            // 元の応答テキストからタグを削除 (または適切に整形)
            finalAiReplyText = aiReplyTextWithPotentialTag.Replace(imageTagMatch.Value, "").Trim();
            // 必要であれば「画像を生成したよ」的なメッセージを追加しても良い
            // finalAiReplyText += "\n（画像を生成しました）";

            if (!string.IsNullOrWhiteSpace(imagePrompt))
            {
                try
                {
                    var imageResponse = await _imagenService.GenerateImageAsync(imagePrompt, cancellationToken);

                    if (imageResponse != null)
                    {
                        generatedImageUrl = $"data:{imageResponse.MimeType};base64,{imageResponse.Base64Data}";
                        _logger.LogInformation("Image generated successfully for session {SessionId}", session.Id);
                    }
                    else
                    {
                        _logger.LogWarning("ImagenService returned null for session {SessionId}. Prompt: '{ImagePrompt}'", session.Id, imagePrompt);
                        // 画像生成失敗時の代替テキストを応答に追加してもよい
                        // finalAiReplyText += "\n（画像生成に失敗しました）";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error calling Imagen service for session {SessionId}", session.Id);
                    // 画像生成失敗時の代替テキストを応答に追加してもよい
                    // finalAiReplyText += "\n（画像生成中にエラーが発生しました）";
                }
            }
            else
            {
                _logger.LogWarning("Image generation tag found but prompt was empty for session {SessionId}.", session.Id);
                finalAiReplyText += "\n（画像生成の指示がありましたが、プロンプトが空でした）";
            }
        }


        // 5. AI の応答を保存
        var aiMessage = new ChatMessage
        {
            SessionId = session.Id,
            CharacterProfileId = request.CharacterProfileId,
            UserId = appUserId.Value,
            Sender = "ai",
            Text = finalAiReplyText,
            ImageUrl = generatedImageUrl,
            Timestamp = DateTime.UtcNow,
        };
        _context.ChatMessages.Add(aiMessage);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("AI message saved for session {SessionId}", session.Id);

        // 6. フロントエンドに応答を返す
        var response = new ChatResponse(finalAiReplyText, session.Id, generatedImageUrl);

        return Ok(response);
    }

    private async Task<ChatSession?> GetOrCreateSessionAsync(string? requestedSessionId, int characterId, int userId, CancellationToken cancellationToken)
    {
        ChatSession? session;
        if (!string.IsNullOrEmpty(requestedSessionId))
        {
            session = await _context.ChatSessions
                .FirstOrDefaultAsync(s => s.Id == requestedSessionId && s.CharacterProfileId == characterId && s.UserId == userId, cancellationToken);

            if (session == null || session.EndTime != null) // 見つからない or 終了済み
            {
                _logger.LogWarning("Session ID {SessionId} invalid or ended. Creating new.", requestedSessionId);
                session = await CreateNewSession(characterId, userId, cancellationToken);
            }
            else
            {
                session.UpdatedAt = DateTime.UtcNow;
                // Note: SaveChanges needed if only updating UpdatedAt here. Consider if this update is necessary on every message.
            }
        }
        else
        {
            session = await CreateNewSession(characterId, userId, cancellationToken);
        }
        // SaveChanges for UpdatedAt if needed
        // await _context.SaveChangesAsync(cancellationToken);
        return session;
    }

    // セッションを新規作成するヘルパーメソッド
    private async Task<ChatSession> CreateNewSession(int characterId, int userId, CancellationToken cancellationToken)
    {
        var newSession = new ChatSession
        {
            // Id はエンティティのデフォルト値で UUID が生成される想定
            CharacterProfileId = characterId,
            UserId = userId,
            StartTime = DateTime.UtcNow,
            // EndTime は NULL
            // Metadata は NULL
            // CreatedAt, UpdatedAt はデフォルト値
        };
        _context.ChatSessions.Add(newSession);
        await _context.SaveChangesAsync(cancellationToken); // セッションをDBに保存
        _logger.LogInformation("New session created: {SessionId}", newSession.Id);
        return newSession;
    }
}