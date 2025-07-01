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
using AiRoleplayChat.Backend.Utils;
using static AiRoleplayChat.Backend.Utils.PromptUtils; // PromptUtils の using を確認

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
            .Where(s => s.CharacterProfileId == characterId && s.UserId == appUserId && s.EndTime == null)
            .OrderByDescending(s => s.StartTime)
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
    [ProducesResponseType(typeof(IEnumerable<ChatMessageResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<ChatMessageResponseDto>>> GetChatHistory(
        [FromQuery][Required] string sessionId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received request to get chat history for Session {SessionId}", sessionId);
        var (appUserId, errorResult) = await GetCurrentAppUserIdAsync(cancellationToken);
        if (errorResult != null) return errorResult;

        var sessionExists = await _context.ChatSessions.AnyAsync(s => s.Id == sessionId && s.UserId == appUserId, cancellationToken);
        if (!sessionExists)
        {
            _logger.LogWarning("Session not found: {SessionId}", sessionId);
            return NotFound($"Session with ID {sessionId} not found.");
        }

        var messages = await _context.ChatMessages
                                     .Where(m => m.SessionId == sessionId && m.UserId == appUserId)
                                     .OrderBy(m => m.Timestamp)
                                     .Select(m => new ChatMessageResponseDto
                                     {
                                         Id = m.Id,
                                         Sender = m.Sender,
                                         Text = m.Text,
                                         ImageUrl = m.ImageUrl,
                                         Timestamp = m.Timestamp
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

        var character = await _context.CharacterProfiles.FindAsync(new object[] { request.CharacterProfileId }, cancellationToken);
        if (character == null)
        {
            _logger.LogWarning("Character not found: {CharacterId}", request.CharacterProfileId);
            return NotFound($"Character with ID {request.CharacterProfileId} not found.");
        }

        ChatSession? session = await GetOrCreateSessionAsync(request.SessionId, request.CharacterProfileId, appUserId.Value, cancellationToken);
        if (session == null) return StatusCode(500, "Failed to get or create session.");

        // 1. 過去の履歴をDBから取得
        List<ChatMessage> history = await _context.ChatMessages
                                .Where(m => m.SessionId == session.Id)
                                .OrderBy(m => m.Timestamp) // Geminiに渡すため昇順
                                .AsNoTracking()
                                .ToListAsync(cancellationToken);

        // 2. 今回のユーザー発言をメモリ上にのみ作成 (DBにはまだ保存しない)
        var userMessage = new ChatMessage
        {
            SessionId = session.Id,
            CharacterProfileId = request.CharacterProfileId,
            UserId = appUserId.Value,
            Sender = "user",
            Text = request.Prompt,
            Timestamp = DateTime.UtcNow,
            // CreatedAt, UpdatedAtはDB保存時に自動設定される想定
        };

        // 3. AIに渡すため、取得した履歴に今回の発言を追加
        var historyForGeneration = new List<ChatMessage>(history) { userMessage };

        // 4. GeminiService で応答を取得
        string aiReplyTextWithPotentialTag;
        try
        {
            aiReplyTextWithPotentialTag = await _geminiService.GenerateChatResponseAsync(
                request.Prompt,
                SystemPromptHelper.AppendImageInstruction(character.SystemPrompt ?? SystemPromptHelper.GenerateDefaultPrompt(character.Name, character.Personality, character.Tone, character.Backstory)),
                historyForGeneration, // 今回の発言を含んだ履歴を渡す
                cancellationToken
             );
            _logger.LogInformation("Gemini response received for session {SessionId}", session.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Gemini service for session {SessionId}", session.Id);
            return StatusCode(StatusCodes.Status500InternalServerError, "AI応答の生成中にエラーが発生しました。");
        }

        string finalAiReplyText = aiReplyTextWithPotentialTag;
        string? generatedImageUrl = null;
        Match imageTagMatch = Regex.Match(aiReplyTextWithPotentialTag, @"\[generate_image:\s*(.*?)\]");

        if (imageTagMatch.Success)
        {
            string imagePrompt = imageTagMatch.Groups[1].Value.Trim();
            _logger.LogInformation("Image generation requested by AI for session {SessionId}. Prompt: '{ImagePrompt}'", session.Id, imagePrompt);
            finalAiReplyText = aiReplyTextWithPotentialTag.Replace(imageTagMatch.Value, "").Trim();

            if (!string.IsNullOrWhiteSpace(imagePrompt))
            {
                try
                {
                    generatedImageUrl = await _imagenService.GenerateImageAsync(imagePrompt, cancellationToken);
                    if (!string.IsNullOrEmpty(generatedImageUrl))
                    {
                        _logger.LogInformation("Image generated successfully for session {SessionId}", session.Id);
                    }
                    else
                    {
                         _logger.LogWarning("ImagenService returned null for session {SessionId}. Prompt: '{ImagePrompt}'", session.Id, imagePrompt);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error calling Imagen service for session {SessionId}", session.Id);
                }
            }
            else
            {
                 _logger.LogWarning("Image generation tag found but prompt was empty for session {SessionId}.", session.Id);
                 finalAiReplyText += "\n（画像生成の指示がありましたが、プロンプトが空でした）";
            }
        }

        // 5. AI の応答メッセージを作成
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
        
        // 6. 応答成功後、ユーザーの発言とAIの発言を両方DBに保存
        // (ChatMessageServiceを使わず、直接ContextにAddRangeする)
        _context.ChatMessages.AddRange(userMessage, aiMessage);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("User and AI messages saved for session {SessionId}", session.Id);

        // 7. フロントエンドに応答を返す
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

            if (session == null || session.EndTime != null)
            {
                _logger.LogWarning("Session ID {SessionId} invalid or ended. Creating new.", requestedSessionId);
                session = await CreateNewSession(characterId, userId, cancellationToken);
            }
            else
            {
                session.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
        else
        {
            session = await CreateNewSession(characterId, userId, cancellationToken);
        }
        return session;
    }

    private async Task<ChatSession> CreateNewSession(int characterId, int userId, CancellationToken cancellationToken)
    {
        var newSession = new ChatSession
        {
            CharacterProfileId = characterId,
            UserId = userId,
            StartTime = DateTime.UtcNow,
        };
        _context.ChatSessions.Add(newSession);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("New session created: {SessionId}", newSession.Id);
        return newSession;
    }
}