import { Configuration, LogLevel } from '@azure/msal-browser';

// MSAL の設定オブジェクト
export const msalConfig: Configuration = {
  auth: {
    clientId: import.meta.env.VITE_B2C_CLIENT_ID || '', // 環境変数から読み込む (|| '' は型エラー回避)
    authority: import.meta.env.VITE_B2C_AUTHORITY || '',
    knownAuthorities: [(import.meta.env.VITE_B2C_KNOWN_AUTHORITIES || '')], // 配列にする
    redirectUri: import.meta.env.VITE_B2C_REDIRECT_URI || 'http://localhost:5173', // デフォルト値も指定可
  },
  cache: {
    // トークンをどこにキャッシュするか (sessionStorage or localStorage)
    cacheLocation: 'sessionStorage', // sessionStorage の方が一般的に推奨される
    storeAuthStateInCookie: false, // Cookie は使わない (セキュリティ上の理由から false 推奨)
  },
  system: {
    // デバッグ用のログ設定 (開発中は Verbose, 本番では Warning や Error に)
    loggerOptions: {
      loggerCallback: (level, message, containsPii) => {
        if (containsPii) {
          return;
        }
        switch (level) {
          case LogLevel.Error:
            console.error(message);
            return;
          case LogLevel.Info:
            // console.info(message); // 必要なら Info も表示
            return;
          case LogLevel.Verbose:
            console.debug(message);
            return;
          case LogLevel.Warning:
            console.warn(message);
            return;
          default:
            return;
        }
      },
      logLevel: LogLevel.Verbose // 開発中は詳細ログを出力
    }
  }
};

// API アクセスに必要なスコープを定義
export const loginRequest = {
  scopes: [(import.meta.env.VITE_B2C_API_SCOPE_URI || '')]
  
};