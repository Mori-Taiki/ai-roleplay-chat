using System;
using System.Linq;
using System.Threading.Tasks;
using AiRoleplayChat.Backend.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging; // ロギング追加

namespace AiRoleplayChat.Backend.Services
{
    public class ChatSessionService : IChatSessionService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ChatSessionService> _logger; // ロガー

        public ChatSessionService(AppDbContext context, ILogger<ChatSessionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> DeleteChatSessionAsync(string sessionId, int userId)
        {
            // Include(s => s.Messages) は Cascade Delete が有効なら不要かもしれないが、
            // 明示的に削除する場合や確認のために含めることも検討。
            // まずは Cascade Delete を信頼し、Session のみ取得する。
            var session = await _context.ChatSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session == null)
            {
                _logger.LogWarning("Attempted to delete non-existent session {SessionId}", sessionId);
                return new NotFoundResult(); // 404 Not Found
            }

            // ユーザーIDを検証
            if (session.UserId != userId)
            {
                _logger.LogWarning("User {UserId} attempted to delete session {SessionId} owned by another user.", userId, sessionId);
                return new ForbidResult(); // 403 Forbidden
            }

            try
            {
                // ChatSession を削除。AppDbContext の設定により、関連する ChatMessages も削除されるはず。
                _context.ChatSessions.Remove(session);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully deleted session {SessionId} for user {UserId}", sessionId, userId);
                return new NoContentResult(); // 204 No Content
            }
            catch (DbUpdateException ex) // concurrency などの問題
            {
                _logger.LogError(ex, "Error deleting session {SessionId} for user {UserId}", sessionId, userId);
                // 500 Internal Server Error を返すか、より具体的なエラーを返す
                return new ObjectResult($"An error occurred while deleting the session.") { StatusCode = 500 };
            }
        }
    }
}