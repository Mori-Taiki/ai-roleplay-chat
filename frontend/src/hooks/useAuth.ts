import { useCallback } from 'react';
import { useMsal, useIsAuthenticated } from "@azure/msal-react";
import { InteractionStatus, InteractionRequiredAuthError, AccountInfo } from "@azure/msal-browser";
import { loginRequest } from "../authConfig"; // スコープ定義をインポート
import { getGenericErrorMessage } from '../utils/errorHandler'; // エラーハンドラをインポート

interface AuthHookResult {
  /**
   * API アクセス用のアクセストークンを非同期で取得します。
   * 取得に失敗した場合は null を返します。
   * @returns アクセストークン文字列、または null
   */
  acquireToken: () => Promise<string | null>;
  /**
   * 現在の認証状態 (ログインしているかどうか)
   */
  isAuthenticated: boolean;
  /**
   * ログイン中のユーザーのアカウント情報 (未ログイン時は null)
   */
  user: AccountInfo | null;
  /**
   * MSAL が処理中かどうか
   */
  isMsalLoading: boolean;
}

export const useAuth = (): AuthHookResult => {
  const { instance, accounts, inProgress } = useMsal();
  const isAuthenticated = useIsAuthenticated(); // 組み込みのフックを利用

  // useMsal の inProgress を利用してローディング状態を判断
  const isMsalLoading = inProgress !== InteractionStatus.None;

  // ログイン中のアカウント情報 (最初のものを取得)
  const user = accounts.length > 0 ? accounts[0] : null;

  // アクセストークン取得関数 (useCallback でメモ化)
  const acquireToken = useCallback(async (): Promise<string | null> => {
    if (isMsalLoading) {
      console.warn("useAuth: MSAL interaction is currently in progress.");
      return null;
    }
    if (!user) {
       console.warn("useAuth: No active account found.");
       // ここでエラーをスローするか、単に null を返すかは設計による
       return null;
    }

    const tokenRequest = {
        scopes: loginRequest.scopes,
        account: user,
    };

    try {
        const tokenResponse = await instance.acquireTokenSilent(tokenRequest);
        console.log("useAuth: Access token acquired silently.");
        return tokenResponse.accessToken;
    } catch (error) {
        console.error("useAuth: Silent token acquisition failed:", error);
        if (error instanceof InteractionRequiredAuthError) {
            // 必要ならここで acquireTokenRedirect/Popup を呼ぶ処理を追加できる
            // instance.acquireTokenRedirect(tokenRequest).catch(console.error);
            // 今回はエラーメッセージをログに出すだけにする
            console.error("useAuth: Interaction required to acquire token.");
        } else {
            // その他のエラー
            console.error("useAuth: Token acquisition error - ", getGenericErrorMessage(error, '認証トークンの取得'));
        }
        return null; // エラー時は null を返す
    }
  }, [instance, user, isMsalLoading]); // user と isMsalLoading も依存配列に追加

  return { acquireToken, isAuthenticated, user, isMsalLoading };
};