namespace AiRoleplayChat.Backend.Services;

public interface IApiKeyService
{
    /// <summary>
    /// ユーザーのAPIキーをKey Vaultに保存します
    /// </summary>
    /// <param name="userId">ユーザーID</param>
    /// <param name="serviceName">サービス名（例: "Gemini", "Replicate"）</param>
    /// <param name="apiKey">APIキー</param>
    /// <returns>保存成功時はtrue</returns>
    Task<bool> StoreApiKeyAsync(int userId, string serviceName, string apiKey);

    /// <summary>
    /// ユーザーのAPIキーをKey Vaultから取得します
    /// </summary>
    /// <param name="userId">ユーザーID</param>
    /// <param name="serviceName">サービス名（例: "Gemini", "Replicate"）</param>
    /// <returns>APIキー（見つからない場合はnull）</returns>
    Task<string?> GetApiKeyAsync(int userId, string serviceName);

    /// <summary>
    /// ユーザーのAPIキーをKey Vaultから削除します
    /// </summary>
    /// <param name="userId">ユーザーID</param>
    /// <param name="serviceName">サービス名（例: "Gemini", "Replicate"）</param>
    /// <returns>削除成功時はtrue</returns>
    Task<bool> DeleteApiKeyAsync(int userId, string serviceName);

    /// <param name="userId">ユーザーID</param>
    /// <returns>サービス名の一覧</returns>
    Task<List<string>> GetUserRegisteredServicesAsync(int userId);
}