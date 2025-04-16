using AiRoleplayChat.Backend.Models;
using AiRoleplayChat.Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using AiRoleplayChat.Backend.Domain.Entities;
using AiRoleplayChat.Backend.Data;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;

namespace AiRoleplayChat.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] 
public class ChatController : ControllerBase
{
    private readonly IGeminiService _geminiService;
    private readonly ILogger<ChatController> _logger;
    private readonly AppDbContext _context;
    private const int TEMP_USER_ID = 1; // 仮のユーザーID

    public ChatController(IGeminiService geminiService, ILogger<ChatController> logger, AppDbContext context)
    {
        _geminiService = geminiService;
        _logger = logger;
        _context = context;
    }

    [HttpGet("sessions/latest", Name = "GetLatestActiveSession")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<string>> GetLatestActiveSession(
    [FromQuery][Required] int characterId,
    CancellationToken cancellationToken)
    {
        // 仮のユーザーIDを使用
        const int userId = TEMP_USER_ID;
        _logger.LogInformation("Requesting latest active session for Character {CharacterId} and User {UserId}", characterId, userId);

        var latestSession = await _context.ChatSessions
            .Where(s => s.CharacterProfileId == characterId && s.UserId == userId && s.EndTime == null) // アクティブなセッションを検索
            .OrderByDescending(s => s.StartTime) // 開始時刻が最新のもの
            .FirstOrDefaultAsync(cancellationToken);

        if (latestSession == null)
        {
            _logger.LogInformation("No active session found for Character {CharacterId} and User {UserId}", characterId, userId);
            return NotFound("アクティブなチャットセッションが見つかりません。");
        }

        _logger.LogInformation("Found latest active session: {SessionId}", latestSession.Id);
        return Ok(latestSession.Id); // セッションID (文字列) を返す
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

        // 0. セッションが存在するか確認
        var sessionExists = await _context.ChatSessions.AnyAsync(s => s.Id == sessionId, cancellationToken);
        if (!sessionExists)
        {
            _logger.LogWarning("Session not found: {SessionId}", sessionId);
            return NotFound($"Session with ID {sessionId} not found.");
        }

        // 1. 指定された sessionId に紐づくメッセージを Timestamp の昇順で取得
        var messages = await _context.ChatMessages
                                     .Where(m => m.SessionId == sessionId)
                                     .OrderBy(m => m.Timestamp) // 古い順に取得
                                                                // .Take(100) // 必要なら取得件数に上限を設ける
                                     .Select(m => new ChatMessageResponseDto // ★ エンティティを直接返さず DTO にマッピング推奨
                                     {
                                         Id = m.Id, // フロントの Message 型に合わせて string にしても良い
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
    [HttpPost(Name = "PostChatMessage")] // アクション名を変更推奨
    [ProducesResponseType(typeof(ChatResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)] // Character が見つからない場合
    public async Task<ActionResult<ChatResponse>> PostChatMessage([FromBody] ChatRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received chat request for Character {CharacterId}, Session {SessionId}", request.CharacterProfileId, request.SessionId ?? "New");

        // 0. キャラクターが存在するか確認
        var character = await _context.CharacterProfiles.FindAsync(new object[] { request.CharacterProfileId }, cancellationToken);
        if (character == null)
        {
            _logger.LogWarning("Character not found: {CharacterId}", request.CharacterProfileId);
            return NotFound($"Character with ID {request.CharacterProfileId} not found.");
        }

        // 1. セッションを特定または新規作成
        ChatSession? session;
        if (!string.IsNullOrEmpty(request.SessionId))
        {
            // 提供された SessionId で検索
            session = await _context.ChatSessions
                                    .FirstOrDefaultAsync(s => s.Id == request.SessionId
                                    && s.CharacterProfileId == request.CharacterProfileId
                                    && s.UserId == TEMP_USER_ID, cancellationToken);

            if (session == null)
            {
                _logger.LogWarning("Session ID {SessionId} provided but not found or invalid for character {CharacterId}. Creating a new session.", request.SessionId, request.CharacterProfileId);
                // 見つからない or 不正な場合は新しいセッションを作成（フォールバック）
                session = await CreateNewSession(request.CharacterProfileId, TEMP_USER_ID, cancellationToken);
            }
            else if (session.EndTime != null)
            {
                _logger.LogWarning("Session ID {SessionId} provided but it has already ended. Creating a new session.", request.SessionId);
                // 終了済みのセッションIDが指定された場合も新規作成
                session = await CreateNewSession(request.CharacterProfileId, TEMP_USER_ID, cancellationToken);
            }
            else
            {
                _logger.LogInformation("Continuing existing session: {SessionId}", session.Id);
                // 既存セッションの最終更新日時を更新 (任意)
                session.UpdatedAt = DateTime.UtcNow;
                // _context.ChatSessions.Update(session); // SaveChangesAsync 前なので不要な場合も
            }
        }
        else
        {
            // SessionId が提供されなかったので新規作成
            _logger.LogInformation("No SessionId provided. Creating a new session for character {CharacterId}", request.CharacterProfileId);
            session = await CreateNewSession(request.CharacterProfileId, TEMP_USER_ID, cancellationToken);
        }

        // 2. ユーザーの発言を保存
        var userMessage = new ChatMessage
        {
            SessionId = session.Id,
            CharacterProfileId = request.CharacterProfileId,
            UserId = TEMP_USER_ID,
            Sender = "user",
            Text = request.Prompt,
            Timestamp = DateTime.UtcNow, // 発言時刻
            // CreatedAt, UpdatedAt はデフォルト値を使用
        };
        _context.ChatMessages.Add(userMessage);
        await _context.SaveChangesAsync(cancellationToken); // ユーザーメッセージを保存確定
        _logger.LogInformation("User message saved for session {SessionId}", session.Id);


        // 3. (TODO - 次のステップ) 履歴を取得
        var history = await _context.ChatMessages
                                .Where(m => m.SessionId == session.Id)
                                .OrderBy(m => m.Timestamp) // 古い順
                                .Take(20) // 例: 直近20件 (トークン数考慮が必要)
                                .ToListAsync(cancellationToken);
        // var formattedHistory = FormatHistoryForGemini(history);

        // 4. GeminiService で応答を取得 (履歴はまだ渡さない)
        string aiReplyText;
        try
        {
            // ★ GeminiService に履歴を渡すように修正する必要あり
            aiReplyText = await _geminiService.GenerateChatResponseAsync(
                request.Prompt,
                character.SystemPrompt ?? "Default system prompt", // キャラクターのシステムプロンプトを使用
                history, // ★ 履歴を渡す
                cancellationToken
             );
            _logger.LogInformation("Gemini response received for session {SessionId}", session.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Gemini service for session {SessionId}", session.Id);
            return StatusCode(StatusCodes.Status500InternalServerError, "AI応答の生成中にエラーが発生しました。");
        }


        // 5. AI の応答を保存
        var aiMessage = new ChatMessage
        {
            SessionId = session.Id,
            CharacterProfileId = request.CharacterProfileId,
            UserId = TEMP_USER_ID, // AIの発言だが、セッションのユーザーに関連付ける
            Sender = "ai",
            Text = aiReplyText,
            Timestamp = DateTime.UtcNow,
        };
        _context.ChatMessages.Add(aiMessage);
        await _context.SaveChangesAsync(cancellationToken); // AIメッセージを保存確定
        _logger.LogInformation("AI message saved for session {SessionId}", session.Id);

        // 6. フロントエンドに応答を返す
        var response = new ChatResponse(aiReplyText, session.Id);

        return Ok(response);
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