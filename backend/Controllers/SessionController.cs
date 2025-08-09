// backend/Controllers/SessionController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AiRoleplayChat.Backend.Services; // IChatSessionService
using AiRoleplayChat.Backend.Models;
using System.Linq;

namespace AiRoleplayChat.Backend.Controllers // プロジェクトの実際の名前空間に合わせる
{
    [Authorize] // このコントローラーのアクションは認証が必要
    [ApiController]
    [Route("api/[controller]")] // ルートは /api/sessions になる
    public class SessionsController : BaseApiController // BaseApiController を継承して UserId を取得
    {
        private readonly IChatSessionService _chatSessionService;
        // private readonly ILogger<SessionsController> _logger; // BaseApiController がロガーを持つ場合、こちらで重複して持つ必要はないかも

        // ★ BaseApiController のコンストラクタに合わせて IUserService と ILogger を受け取る必要がある
        public SessionsController(
            IChatSessionService chatSessionService,
            IUserService userService, // BaseApiController に必要
            ILogger<SessionsController> logger) // BaseApiController に必要 (型パラメータは SessionsController に)
            : base(userService, logger) // BaseApiController のコンストラクタを呼び出す
        {
            _chatSessionService = chatSessionService ?? throw new ArgumentNullException(nameof(chatSessionService));
        }

        // GET: api/sessions/character/{characterId}
        [HttpGet("character/{characterId}", Name = "GetSessionsForCharacter")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<ChatSessionResponseDto>>> GetSessionsForCharacter(int characterId)
        {
            var (appUserId, errorResult) = await GetCurrentAppUserIdAsync();
            if (errorResult != null)
            {
                return errorResult;
            }
            if (appUserId == null)
            {
                _logger.LogWarning("User ID could not be determined for sessions fetch for character: {CharacterId}", characterId);
                return Unauthorized("User ID could not be determined.");
            }

            try
            {
                var sessions = await _chatSessionService.GetSessionsForCharacterAsync(characterId, (int)appUserId);
                
                var sessionDtos = sessions.Select(s => new ChatSessionResponseDto
                {
                    Id = s.Id,
                    CharacterProfileId = s.CharacterProfileId,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    CreatedAt = s.CreatedAt,
                    UpdatedAt = s.UpdatedAt,
                    LastMessageSnippet = s.Messages?.FirstOrDefault()?.Text,
                    MessageCount = s.Messages?.Count ?? 0
                }).ToList();

                return Ok(sessionDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sessions for character {CharacterId} and user {UserId}", characterId, appUserId);
                return StatusCode(500, "An error occurred while retrieving sessions.");
            }
        }

        // POST: api/sessions/character/{characterId}
        [HttpPost("character/{characterId}", Name = "CreateNewSession")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ChatSessionResponseDto>> CreateNewSession(int characterId)
        {
            var (appUserId, errorResult) = await GetCurrentAppUserIdAsync();
            if (errorResult != null)
            {
                return errorResult;
            }
            if (appUserId == null)
            {
                _logger.LogWarning("User ID could not be determined for session creation for character: {CharacterId}", characterId);
                return Unauthorized("User ID could not be determined.");
            }

            try
            {
                var session = await _chatSessionService.CreateNewSessionAsync(characterId, (int)appUserId);
                
                var sessionDto = new ChatSessionResponseDto
                {
                    Id = session.Id,
                    CharacterProfileId = session.CharacterProfileId,
                    StartTime = session.StartTime,
                    EndTime = session.EndTime,
                    CreatedAt = session.CreatedAt,
                    UpdatedAt = session.UpdatedAt,
                    LastMessageSnippet = null,
                    MessageCount = 0
                };

                return CreatedAtAction(
                    nameof(GetSessionsForCharacter),
                    new { characterId = session.CharacterProfileId },
                    sessionDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating session for character {CharacterId} and user {UserId}", characterId, appUserId);
                return StatusCode(500, "An error occurred while creating the session.");
            }
        }

        // DELETE: api/sessions/{sessionId}
        [HttpDelete("{sessionId}", Name = "DeleteChatSession")] // ルートパラメータ名とメソッド引数名を一致させる
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteSession(string sessionId) // メソッド名を DeleteSession に変更
        {
            // BaseApiController から UserId を取得 (非同期メソッドを使用)
            var (appUserId, errorResult) = await GetCurrentAppUserIdAsync(); // CancellationToken は任意
            if (errorResult != null)
            {
                // GetCurrentAppUserIdAsync がエラーを返した場合 (例: Unauthorized)
                return errorResult;
            }
            if (appUserId == null)
            {
                // 通常は GetCurrentAppUserIdAsync 内で Unauthorized が返されるはずだが念のため
                 _logger.LogWarning("User ID could not be determined for session deletion attempt: {SessionId}", sessionId);
                return Unauthorized("User ID could not be determined.");
            }

            // サービスを呼び出してセッション削除を実行
            var result = await _chatSessionService.DeleteChatSessionAsync(sessionId, (int)appUserId);

            // サービスが返した IActionResult をそのまま返す
            return result;
        }

        // 必要であれば、セッション一覧取得 (GET /api/sessions) や
        // 特定セッション情報取得 (GET /api/sessions/{sessionId}) などのエンドポイントも
        // 将来的にこのコントローラーに追加できます。
    }
}