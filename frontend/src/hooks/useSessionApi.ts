// src/hooks/useSessionApi.ts (新規作成)
import { useCallback } from 'react';
import { useAuth } from './useAuth'; // 認証フック
import { getApiErrorMessage, getGenericErrorMessage } from '../utils/errorHandler'; // エラーハンドラ

// フックが返すオブジェクトの型 (将来的に他の関数を追加する可能性も考慮)
interface SessionApiHook {
  deleteSession: (sessionId: string) => Promise<void>;
}

export const useSessionApi = (): SessionApiHook => {
  const { acquireToken } = useAuth();

  // セッション削除関数
  const deleteSession = useCallback(async (sessionId: string): Promise<void> => {
    const accessToken = await acquireToken();
    if (!accessToken) {
      // トークンがなければエラーを投げるか、特定の処理を行う
      throw new Error("認証トークンが取得できませんでした。再度ログインしてください。");
    }

    console.log(`Attempting to delete session: ${sessionId}`); // デバッグログ

    try {
      // ★ 正しいエンドポイント /api/sessions/{sessionId} を指定
      const response = await fetch(`/api/sessions/${sessionId}`, {
        method: 'DELETE',
        headers: {
          'Authorization': `Bearer ${accessToken}`,
          // 必要に応じて他のヘッダー (Content-Type など) を追加するが、DELETE では通常不要
        },
      });

      // ステータスコード 204 No Content が成功を示す
      if (response.status === 204) {
        console.log(`Session ${sessionId} deleted successfully.`); // デバッグログ
        return; // 成功
      } else {
        // 204 以外のステータスコードはエラーとして扱う
        const message = await getApiErrorMessage(response); // エラーレスポンスからメッセージ取得試行
        console.error(`Failed to delete session ${sessionId}. Status: ${response.status}, Message: ${message}`); // デバッグログ
        throw new Error(message || `セッションの削除に失敗しました (Status: ${response.status})`);
      }
    } catch (err) {
        // fetch 自体の失敗や、上記以外の予期せぬエラー
        const message = getGenericErrorMessage(err, 'セッションの削除');
        console.error('Error during session deletion fetch:', err); // デバッグログ
        throw new Error(message); // 処理済みのエラーを再スロー
    }
  }, [acquireToken]); // acquireToken に依存

  // フックが返すオブジェクト
  return {
    deleteSession,
    // 他のセッション関連API関数をここに追加可能
  };
};