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
}