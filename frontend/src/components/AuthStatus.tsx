import React from "react";
import { useMsal, useIsAuthenticated } from "@azure/msal-react";
import { InteractionStatus } from "@azure/msal-browser";
import Button from "./Button"; // 作成済みの Button コンポーネントを使用
import { loginRequest } from "../authConfig"; // スコープ定義をインポート

export const AuthStatus: React.FC = () => {
  const { instance, accounts, inProgress } = useMsal(); // MSAL のインスタンス、アカウント情報、処理中ステータスを取得
  const isAuthenticated = useIsAuthenticated(); // 簡易的な認証状態チェックフック

  const handleLoginRedirect = () => {
    instance.loginRedirect(loginRequest).catch(e => {
      console.error("Login redirect failed:", e);
      // 必要であればユーザーにエラーメッセージを表示
    });
  };

  const handleLogoutRedirect = () => {
    // ログアウト時に特定のアカウントを指定することも可能
    // instance.logoutRedirect({ account: accounts[0] });
    instance.logoutRedirect({ postLogoutRedirectUri: "/" }); // ログアウト後にトップページにリダイレクト
  };

  // ユーザー名を取得 (複数のアカウントが返る可能性は低いが、最初のものを利用)
  const username = accounts.length > 0 ? accounts[0].name : undefined;

  // MSAL が何らかの処理中 (リダイレクト処理中など) かどうか
  const isMsalProcessing = inProgress !== InteractionStatus.None;

  return (
    <div style={{ marginLeft: 'auto', display: 'flex', alignItems: 'center', gap: '0.5rem' }}> {/* 右寄せなどのスタイルは適宜調整 */}
      {isAuthenticated ? (
        <>
          <span>こんにちは、{username || 'ゲスト'} さん</span>
          <Button onClick={handleLogoutRedirect} variant="secondary" size="sm" disabled={isMsalProcessing}>
            ログアウト
          </Button>
        </>
      ) : (
        <Button onClick={handleLoginRedirect} variant="primary" size="sm" disabled={isMsalProcessing}>
          ログイン / 新規登録
        </Button>
      )}
    </div>
  );
};