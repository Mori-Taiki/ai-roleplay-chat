using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc; // ActionResultなどを返すなら
using AiRoleplayChat.Backend.Domain.Entities;

namespace AiRoleplayChat.Backend.Services
{
    // 戻り値は bool や enum などでも良いですが、コントローラーで使いやすいよう ActionResult を返す例
    public interface IChatSessionService
    {
        Task<IActionResult> DeleteChatSessionAsync(string sessionId, int userId);
        Task<List<ChatSession>> GetSessionsForCharacterAsync(int characterId, int userId);
        Task<ChatSession> CreateNewSessionAsync(int characterId, int userId);
    }
}