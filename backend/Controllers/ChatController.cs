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


        // 3. GeminiService で応答を取得
        string aiReplyTextWithPotentialTag;
        try
        {
            aiReplyTextWithPotentialTag = await _geminiService.GenerateChatResponseAsync(
                request.Prompt,
                SystemPromptHelper.AppendImageInstruction(character.SystemPrompt ?? SystemPromptHelper.GenerateDefaultPrompt(character.Name, character.Personality, character.Tone, character.Backstory)),
                history,
                appUserId,
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
        bool requiresImageGeneration = false;

        // 4. 応答に画像生成タグがあるかチェック
        Match imageTagMatch = Regex.Match(aiReplyTextWithPotentialTag, @"\[generate_image\]");
        if (imageTagMatch.Success)
        {
            requiresImageGeneration = true;
            // 応答テキストからは画像生成タグを削除
            finalAiReplyText = aiReplyTextWithPotentialTag.Replace(imageTagMatch.Value, "").Trim();
            _logger.LogInformation("Image generation tag detected for session {SessionId}.", session.Id);
        }


        // 5. AI の応答メッセージを作成
        var aiMessage = new ChatMessage
        {
            SessionId = session.Id,
            CharacterProfileId = request.CharacterProfileId,
            UserId = appUserId.Value,
            Sender = "ai",
            Text = finalAiReplyText,
            ImageUrl = null,
            Timestamp = DateTime.UtcNow,
        };

        // AIの発言をDBに保存
        _context.ChatMessages.AddRange(userMessage, aiMessage);
        await _context.SaveChangesAsync(cancellationToken);

        // フロントエンドに応答を返す
        var response = new ChatResponse(
            finalAiReplyText,
            session.Id,
            aiMessage.Id,
            requiresImageGeneration
        );

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

    // PUT /api/chat/messages/{userMessageId}/edit-and-regenerate
    [HttpPut("messages/{userMessageId}/edit-and-regenerate")]
    [ProducesResponseType(typeof(ChatResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ChatResponse>> EditAndRegenerateAsync(
        int userMessageId,
        [FromBody] EditMessageRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received edit and regenerate request for message {MessageId}", userMessageId);

        var (appUserId, errorResult) = await GetCurrentAppUserIdAsync(cancellationToken);
        if (errorResult != null) return errorResult;
        if (appUserId == null) return BadRequest("User ID cannot be null.");

        // Find the user message
        var userMessage = await _context.ChatMessages
            .FirstOrDefaultAsync(m => m.Id == userMessageId && m.UserId == appUserId && m.Sender == "user", cancellationToken);

        if (userMessage == null)
        {
            _logger.LogWarning("User message not found or access denied: {MessageId}", userMessageId);
            return NotFound("指定されたメッセージが見つかりません。");
        }

        // Verify this is the latest user message in the session
        var latestUserMessage = await _context.ChatMessages
            .Where(m => m.SessionId == userMessage.SessionId && m.UserId == appUserId && m.Sender == "user")
            .OrderByDescending(m => m.Timestamp)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestUserMessage == null || latestUserMessage.Id != userMessageId)
        {
            _logger.LogWarning("Attempt to edit non-latest user message: {MessageId}", userMessageId);
            return BadRequest("編集できるのは最新のユーザーメッセージのみです。");
        }

        // Get character info
        var character = await _context.CharacterProfiles.FindAsync(new object[] { userMessage.CharacterProfileId }, cancellationToken);
        if (character == null)
        {
            _logger.LogWarning("Character not found: {CharacterId}", userMessage.CharacterProfileId);
            return NotFound("キャラクターが見つかりません。");
        }

        // Update the user message text
        userMessage.Text = request.NewText;
        userMessage.UpdatedAt = DateTime.UtcNow;

        // Find and remove any existing AI response to this user message
        var existingAiResponse = await _context.ChatMessages
            .Where(m => m.SessionId == userMessage.SessionId && m.UserId == appUserId && m.Sender == "ai")
            .OrderByDescending(m => m.Timestamp)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingAiResponse != null)
        {
            _context.ChatMessages.Remove(existingAiResponse);
        }

        // Get chat history up to the edited message
        var history = await _context.ChatMessages
            .Where(m => m.SessionId == userMessage.SessionId && m.Timestamp < userMessage.Timestamp)
            .OrderBy(m => m.Timestamp)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // Generate new AI response
        string aiReplyTextWithPotentialTag;
        try
        {
            aiReplyTextWithPotentialTag = await _geminiService.GenerateChatResponseAsync(
                request.NewText,
                SystemPromptHelper.AppendImageInstruction(character.SystemPrompt ?? SystemPromptHelper.GenerateDefaultPrompt(character.Name, character.Personality, character.Tone, character.Backstory)),
                history,
                appUserId,
                cancellationToken
            );
            _logger.LogInformation("Gemini response received for edited message {MessageId}", userMessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Gemini service for edited message {MessageId}", userMessageId);
            return StatusCode(StatusCodes.Status500InternalServerError, "AI応答の生成中にエラーが発生しました。");
        }

        string finalAiReplyText = aiReplyTextWithPotentialTag;
        bool requiresImageGeneration = false;

        // Check for image generation tag
        Match imageTagMatch = Regex.Match(aiReplyTextWithPotentialTag, @"\[generate_image\]");
        if (imageTagMatch.Success)
        {
            requiresImageGeneration = true;
            finalAiReplyText = aiReplyTextWithPotentialTag.Replace(imageTagMatch.Value, "").Trim();
            _logger.LogInformation("Image generation tag detected for edited message {MessageId}.", userMessageId);
        }

        // Create new AI response
        var newAiMessage = new ChatMessage
        {
            SessionId = userMessage.SessionId,
            CharacterProfileId = userMessage.CharacterProfileId,
            UserId = appUserId.Value,
            Sender = "ai",
            Text = finalAiReplyText,
            ImageUrl = null,
            Timestamp = DateTime.UtcNow,
        };

        _context.ChatMessages.Add(newAiMessage);
        await _context.SaveChangesAsync(cancellationToken);

        var response = new ChatResponse(
            finalAiReplyText,
            userMessage.SessionId,
            newAiMessage.Id,
            requiresImageGeneration
        );

        return Ok(response);
    }

    // POST /api/chat/ai/{aiMessageId}/regenerate
    [HttpPost("ai/{aiMessageId}/regenerate")]
    [ProducesResponseType(typeof(ChatResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ChatResponse>> RegenerateAiResponseAsync(
        int aiMessageId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received regenerate request for AI message {MessageId}", aiMessageId);

        var (appUserId, errorResult) = await GetCurrentAppUserIdAsync(cancellationToken);
        if (errorResult != null) return errorResult;
        if (appUserId == null) return BadRequest("User ID cannot be null.");

        // Find the AI message
        var aiMessage = await _context.ChatMessages
            .FirstOrDefaultAsync(m => m.Id == aiMessageId && m.UserId == appUserId && m.Sender == "ai", cancellationToken);

        if (aiMessage == null)
        {
            _logger.LogWarning("AI message not found or access denied: {MessageId}", aiMessageId);
            return NotFound("指定されたメッセージが見つかりません。");
        }

        // Verify this is the latest AI message in the session
        var latestAiMessage = await _context.ChatMessages
            .Where(m => m.SessionId == aiMessage.SessionId && m.UserId == appUserId && m.Sender == "ai")
            .OrderByDescending(m => m.Timestamp)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestAiMessage == null || latestAiMessage.Id != aiMessageId)
        {
            _logger.LogWarning("Attempt to regenerate non-latest AI message: {MessageId}", aiMessageId);
            return BadRequest("再生成できるのは最新のAIメッセージのみです。");
        }

        // Get the corresponding user message
        var userMessage = await _context.ChatMessages
            .Where(m => m.SessionId == aiMessage.SessionId && m.UserId == appUserId && m.Sender == "user")
            .OrderByDescending(m => m.Timestamp)
            .FirstOrDefaultAsync(cancellationToken);

        if (userMessage == null)
        {
            _logger.LogWarning("No user message found for AI message regeneration: {MessageId}", aiMessageId);
            return BadRequest("対応するユーザーメッセージが見つかりません。");
        }

        // Get character info
        var character = await _context.CharacterProfiles.FindAsync(new object[] { aiMessage.CharacterProfileId }, cancellationToken);
        if (character == null)
        {
            _logger.LogWarning("Character not found: {CharacterId}", aiMessage.CharacterProfileId);
            return NotFound("キャラクターが見つかりません。");
        }

        // Get chat history up to the user message
        var history = await _context.ChatMessages
            .Where(m => m.SessionId == aiMessage.SessionId && m.Timestamp < userMessage.Timestamp)
            .OrderBy(m => m.Timestamp)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // Generate new AI response
        string aiReplyTextWithPotentialTag;
        try
        {
            aiReplyTextWithPotentialTag = await _geminiService.GenerateChatResponseAsync(
                userMessage.Text,
                SystemPromptHelper.AppendImageInstruction(character.SystemPrompt ?? SystemPromptHelper.GenerateDefaultPrompt(character.Name, character.Personality, character.Tone, character.Backstory)),
                history,
                appUserId,
                cancellationToken
            );
            _logger.LogInformation("Gemini response received for regenerated AI message {MessageId}", aiMessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Gemini service for AI message regeneration {MessageId}", aiMessageId);
            return StatusCode(StatusCodes.Status500InternalServerError, "AI応答の生成中にエラーが発生しました。");
        }

        string finalAiReplyText = aiReplyTextWithPotentialTag;
        bool requiresImageGeneration = false;

        // Check for image generation tag
        Match imageTagMatch = Regex.Match(aiReplyTextWithPotentialTag, @"\[generate_image\]");
        if (imageTagMatch.Success)
        {
            requiresImageGeneration = true;
            finalAiReplyText = aiReplyTextWithPotentialTag.Replace(imageTagMatch.Value, "").Trim();
            _logger.LogInformation("Image generation tag detected for regenerated AI message {MessageId}.", aiMessageId);
        }

        // Create a new AI message instead of updating the existing one
        var newAiMessage = new ChatMessage
        {
            SessionId = aiMessage.SessionId,
            CharacterProfileId = aiMessage.CharacterProfileId,
            UserId = aiMessage.UserId,
            Sender = "ai",
            Text = finalAiReplyText,
            ImageUrl = null,
            Timestamp = DateTime.UtcNow,
        };

        _context.ChatMessages.Add(newAiMessage);
        await _context.SaveChangesAsync(cancellationToken);

        var response = new ChatResponse(
            finalAiReplyText,
            newAiMessage.SessionId,
            newAiMessage.Id,
            requiresImageGeneration
        );

        return Ok(response);
    }
}