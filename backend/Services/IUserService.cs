namespace AiRoleplayChat.Backend.Services; // または Interfaces

public interface IUserService
{
    /// <summary>
    /// 指定された B2C Object ID に対応するアプリケーション内部の User ID を取得または作成します。
    /// </summary>
    /// <param name="b2cObjectId">Azure AD B2C トークンから取得した Object ID (例: User.FindFirstValue(ClaimTypes.NameIdentifier))。</param>
    /// <param name="displayName">トークンから取得した表示名 (新規登録時に使用)。</param>
    /// <param name="email">トークンから取得したメールアドレス (新規登録時に使用、任意)。</param>
    /// <param name="cancellationToken">CancellationToken。</param>
    /// <returns>アプリケーション内部で使う User ID (int)。ユーザーが見つからない/作成できない場合は例外をスローするか、0 などを返すかは要検討。</returns>
    Task<int> GetOrCreateAppUserIdAsync(string b2cObjectId, string displayName, string? email, CancellationToken cancellationToken = default);
}