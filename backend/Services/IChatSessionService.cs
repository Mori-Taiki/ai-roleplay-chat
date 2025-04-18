using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc; // ActionResultなどを返すなら

namespace AiRoleplayChat.Backend.Services
{
    // 戻り値は bool や enum などでも良いですが、コントローラーで使いやすいよう ActionResult を返す例
    public interface IChatSessionService
    {
        Task<IActionResult> DeleteChatSessionAsync(string sessionId, int userId);
    }
}