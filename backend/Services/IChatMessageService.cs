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

    // 必要であれば、メッセージ取得や削除などの他のメソッドもここに追加できます。
    // Task<IEnumerable<ChatMessage>> GetMessagesBySessionIdAsync(string sessionId, CancellationToken cancellationToken = default);
}