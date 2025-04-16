using AiRoleplayChat.Backend.Services; // IUserService
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims; // ClaimTypes
using System.Threading;
using System.Threading.Tasks;

namespace AiRoleplayChat.Backend.Controllers; // または適切な名前空間

[ApiController]
[Authorize]
[Route("api/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    private readonly IUserService _userService;
    protected readonly ILogger _logger;

    protected BaseApiController(IUserService userService, ILogger logger)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 現在認証されているユーザーのアプリケーション内部 User ID を取得または作成します。
    /// 失敗した場合は、適切な ActionResult (Unauthorized or StatusCode 500) を返します。
    /// </summary>
    /// <param name="cancellationToken">CancellationToken。</param>
    /// <returns>成功した場合はアプリ内 User ID (int)。失敗した場合は null とエラーを示す ActionResult。</returns>
    protected async Task<(int? AppUserId, ActionResult? ErrorResult)> GetCurrentAppUserIdAsync(CancellationToken cancellationToken = default)
    {
        var b2cObjectId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(b2cObjectId))
        {
            _logger.LogWarning("Could not find B2C Object ID in claims for the current user.");
            // ControllerBase のヘルパーメソッドを使って ActionResult を返す
            return (null, Unauthorized("ユーザー識別子が見つかりません。"));
        }

        var displayName = User.FindFirstValue("name") ?? "不明なユーザー";
        var email = User.FindFirstValue("emails");

        try
        {
            var appUserId = await _userService.GetOrCreateAppUserIdAsync(b2cObjectId, displayName, email, cancellationToken);
            // 成功した場合は ID を返し、エラーは null
            return (appUserId, null);
        }
        catch (Exception ex)
        {
             _logger.LogError(ex, "Failed to get or create app user for B2C Object ID {B2cObjectId}", b2cObjectId);
             // ControllerBase のヘルパーメソッドを使って ActionResult を返す
             return (null, StatusCode(StatusCodes.Status500InternalServerError, "ユーザー情報の処理中にエラーが発生しました。"));
        }
    }
}