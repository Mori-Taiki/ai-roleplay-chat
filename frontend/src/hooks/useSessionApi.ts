// src/hooks/useSessionApi.ts (新規作成)
import { useCallback } from 'react';
import { useAuth } from './useAuth'; // 認証フック
import { getApiErrorMessage, getGenericErrorMessage } from '../utils/errorHandler'; // エラーハンドラ
import { ChatSessionResponse } from '../models/ChatSessionResponse';

// フックが返すオブジェクトの型 (将来的に他の関数を追加する可能性も考慮)
interface SessionApiHook {
  deleteSession: (sessionId: string) => Promise<void>;
  getSessionsForCharacter: (characterId: number) => Promise<ChatSessionResponse[]>;
  createNewSession: (characterId: number) => Promise<ChatSessionResponse>;
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
      const baseUrl = import.meta.env.VITE_API_URL || '';
      // ★ 正しいエンドポイント /api/sessions/{sessionId} を指定
      const response = await fetch(`${baseUrl}/api/sessions/${sessionId}`, {
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

  // キャラクターのセッション一覧取得関数
  const getSessionsForCharacter = useCallback(async (characterId: number): Promise<ChatSessionResponse[]> => {
    const accessToken = await acquireToken();
    if (!accessToken) {
      throw new Error("認証トークンが取得できませんでした。再度ログインしてください。");
    }

    try {
      const baseUrl = import.meta.env.VITE_API_URL || '';
      const response = await fetch(`${baseUrl}/api/sessions/character/${characterId}`, {
        method: 'GET',
        headers: {
          'Authorization': `Bearer ${accessToken}`,
          'Content-Type': 'application/json',
        },
      });

      if (response.ok) {
        const sessions: ChatSessionResponse[] = await response.json();
        return sessions;
      } else {
        const message = await getApiErrorMessage(response);
        throw new Error(message || `セッション一覧の取得に失敗しました (Status: ${response.status})`);
      }
    } catch (err) {
      const message = getGenericErrorMessage(err, 'セッション一覧の取得');
      console.error('Error during sessions fetch:', err);
      throw new Error(message);
    }
  }, [acquireToken]);

  // 新しいセッション作成関数
  const createNewSession = useCallback(async (characterId: number): Promise<ChatSessionResponse> => {
    const accessToken = await acquireToken();
    if (!accessToken) {
      throw new Error("認証トークンが取得できませんでした。再度ログインしてください。");
    }

    try {
      const baseUrl = import.meta.env.VITE_API_URL || '';
      const response = await fetch(`${baseUrl}/api/sessions/character/${characterId}`, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${accessToken}`,
          'Content-Type': 'application/json',
        },
      });

      if (response.status === 201) {
        const session: ChatSessionResponse = await response.json();
        return session;
      } else {
        const message = await getApiErrorMessage(response);
        throw new Error(message || `新しいセッションの作成に失敗しました (Status: ${response.status})`);
      }
    } catch (err) {
      const message = getGenericErrorMessage(err, '新しいセッションの作成');
      console.error('Error during session creation:', err);
      throw new Error(message);
    }
  }, [acquireToken]);

  // フックが返すオブジェクト
  return {
    deleteSession,
    getSessionsForCharacter,
    createNewSession,
    // 他のセッション関連API関数をここに追加可能
  };
};