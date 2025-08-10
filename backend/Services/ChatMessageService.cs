using AiRoleplayChat.Backend.Data; // AppDbContext
using AiRoleplayChat.Backend.Domain.Entities; // ChatMessage
using Microsoft.EntityFrameworkCore; // DbUpdateException など
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AiRoleplayChat.Backend.Services;

public class ChatMessageService : IChatMessageService
{
    private readonly AppDbContext _context;
    private readonly ILogger<ChatMessageService> _logger;

    public ChatMessageService(AppDbContext context, ILogger<ChatMessageService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ChatMessage> AddMessageAsync(
        string sessionId,
        int characterId,
        int userId,
        string sender,
        string text,
        string? imageUrl,
        DateTime timestamp,
        CancellationToken cancellationToken = default)
    {
        // 簡単なバリデーション (より厳密なバリデーションはモデルやコントローラーで行うべき)
        if (string.IsNullOrEmpty(sessionId) || characterId <= 0 || userId <= 0 || string.IsNullOrEmpty(sender) || string.IsNullOrEmpty(text))
        {
            // 本来はより詳細な例外をスローすべき
            throw new ArgumentException("必須のメッセージ情報が不足しています。");
        }

        var newMessage = new ChatMessage
        {
            SessionId = sessionId,
            CharacterProfileId = characterId,
            UserId = userId,
            Sender = sender,
            Text = text,
            ImageUrl = imageUrl, // null も許容
            Timestamp = timestamp,
            CreatedAt = DateTime.UtcNow, // レコード作成日時はここで設定
            UpdatedAt = DateTime.UtcNow
        };

        try
        {
            _context.ChatMessages.Add(newMessage);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("ChatMessage saved successfully. SessionId: {SessionId}, MessageId: {MessageId}", sessionId, newMessage.Id);
            return newMessage; // 保存されたエンティティ (Id が採番されている) を返す
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Failed to save ChatMessage. SessionId: {SessionId}, Sender: {Sender}", sessionId, sender);
            // データベース保存時の例外を再スローするか、カスタム例外にラップする
            throw new Exception("データベースへのメッセージ保存中にエラーが発生しました。", ex);
        }
    }

    public async Task<ChatMessage> SaveImageMessageAsync(
        int userId,
        int characterId,
        string sessionId,
        string imageUrl,
        string? imagePrompt,
        string? modelId,
        string? serviceName,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0 || characterId <= 0 || string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(imageUrl))
        {
            throw new ArgumentException("必須の画像メッセージ情報が不足しています。");
        }

        var newMessage = new ChatMessage
        {
            SessionId = sessionId,
            CharacterProfileId = characterId,
            UserId = userId,
            Sender = "ai",
            Text = "Generated image",
            ImageUrl = imageUrl,
            ImagePrompt = imagePrompt,
            ModelId = modelId,
            ServiceName = serviceName,
            Timestamp = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        try
        {
            _context.ChatMessages.Add(newMessage);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Image message saved successfully. SessionId: {SessionId}, MessageId: {MessageId}, ModelId: {ModelId}, ServiceName: {ServiceName}", 
                sessionId, newMessage.Id, modelId, serviceName);
            return newMessage;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Failed to save image message. SessionId: {SessionId}, UserId: {UserId}", sessionId, userId);
            throw new Exception("データベースへの画像メッセージ保存中にエラーが発生しました。", ex);
        }
    }

    public async Task<(IEnumerable<ChatMessage> Items, int Total)> GetImageMessagesAsync(
        int userId,
        int characterId,
        string? sessionId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0 || characterId <= 0 || page <= 0 || pageSize <= 0)
        {
            throw new ArgumentException("無効なクエリパラメータです。");
        }

        var query = _context.ChatMessages
            .Where(m => m.UserId == userId 
                     && m.CharacterProfileId == characterId 
                     && m.ImageUrl != null 
                     && m.DeletedAt == null);

        if (!string.IsNullOrEmpty(sessionId))
        {
            query = query.Where(m => m.SessionId == sessionId);
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(m => m.Session)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Retrieved {Count} image messages for UserId: {UserId}, CharacterId: {CharacterId}, SessionId: {SessionId}", 
            items.Count(), userId, characterId, sessionId);

        return (items, total);
    }

    public async Task<bool> SoftDeleteImageMessageAsync(
        int userId,
        int messageId,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0 || messageId <= 0)
        {
            throw new ArgumentException("無効なパラメータです。");
        }

        var message = await _context.ChatMessages
            .Where(m => m.Id == messageId && m.UserId == userId && m.ImageUrl != null && m.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken);

        if (message == null)
        {
            _logger.LogWarning("Image message not found or access denied. MessageId: {MessageId}, UserId: {UserId}", messageId, userId);
            return false;
        }

        try
        {
            message.DeletedAt = DateTime.UtcNow;
            message.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("Image message soft deleted. MessageId: {MessageId}, UserId: {UserId}, CharacterId: {CharacterId}, ModelId: {ModelId}, ServiceName: {ServiceName}", 
                messageId, userId, message.CharacterProfileId, message.ModelId, message.ServiceName);
            return true;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Failed to soft delete image message. MessageId: {MessageId}, UserId: {UserId}", messageId, userId);
            throw new Exception("画像メッセージの削除中にエラーが発生しました。", ex);
        }
    }
}