using AiRoleplayChat.Backend.Domain.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace AiRoleplayChat.Backend.Services;

public interface IChatMessageService
{
    /// <summary>
    /// 新しいチャットメッセージをデータベースに保存します。
    /// </summary>
    /// <param name="sessionId">関連するセッションのID。</param>
    /// <param name="characterId">関連するキャラクターのID。</param>
    /// <param name="userId">メッセージを送信した（または関連する）ユーザーのID。</param>
    /// <param name="sender">'user' または 'ai'。</param>
    /// <param name="text">メッセージ本文。</param>
    /// <param name="imageUrl">画像のURL (任意)。</param>
    /// <param name="timestamp">メッセージのタイムスタンプ。</param>
    /// <param name="cancellationToken">CancellationToken。</param>
    /// <returns>保存された ChatMessage エンティティ。</returns>
    /// <remarks>引数の代わりに CreateChatMessageDto のような DTO を使う設計も可能です。</remarks>
    Task<ChatMessage> AddMessageAsync(
        string sessionId,
        int characterId,
        int userId,
        string sender,
        string text,
        string? imageUrl,
        DateTime timestamp,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 画像メッセージを保存します（画像生成時に使用）。
    /// </summary>
    /// <param name="userId">ユーザーID。</param>
    /// <param name="characterId">キャラクターID。</param>
    /// <param name="sessionId">セッションID。</param>
    /// <param name="imageUrl">画像URL。</param>
    /// <param name="imagePrompt">画像生成に使用したプロンプト。</param>
    /// <param name="modelId">画像生成に使用したモデルID。</param>
    /// <param name="serviceName">画像生成サービス名。</param>
    /// <param name="cancellationToken">CancellationToken。</param>
    /// <returns>保存された ChatMessage エンティティ。</returns>
    Task<ChatMessage> SaveImageMessageAsync(
        int userId,
        int characterId,
        string sessionId,
        string imageUrl,
        string? imagePrompt,
        string? modelId,
        string? serviceName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 画像メッセージを取得します（ページング対応）。
    /// </summary>
    /// <param name="userId">ユーザーID。</param>
    /// <param name="characterId">キャラクターID。</param>
    /// <param name="sessionId">セッションID（null の場合は全セッション）。</param>
    /// <param name="page">ページ番号（1から開始）。</param>
    /// <param name="pageSize">ページサイズ。</param>
    /// <param name="cancellationToken">CancellationToken。</param>
    /// <returns>画像メッセージのリスト（total 付き）。</returns>
    Task<(IEnumerable<ChatMessage> Items, int Total)> GetImageMessagesAsync(
        int userId,
        int characterId,
        string? sessionId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 画像メッセージをソフトデリートします。
    /// </summary>
    /// <param name="userId">ユーザーID。</param>
    /// <param name="messageId">メッセージID。</param>
    /// <param name="cancellationToken">CancellationToken。</param>
    /// <returns>削除が成功したかどうか。</returns>
    Task<bool> SoftDeleteImageMessageAsync(
        int userId,
        int messageId,
        CancellationToken cancellationToken = default);

    // 必要であれば、メッセージ取得や削除などの他のメソッドもここに追加できます。
    // Task<IEnumerable<ChatMessage>> GetMessagesBySessionIdAsync(string sessionId, CancellationToken cancellationToken = default);
}