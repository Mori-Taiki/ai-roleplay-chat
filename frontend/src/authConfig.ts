import { Configuration, LogLevel } from '@azure/msal-browser';

// MSAL の設定オブジェクト
export const msalConfig: Configuration = {
  auth: {
    // ★ フロントエンド SPA 用に登録したアプリのクライアントID
    clientId: '73be69c4-e3f6-4ef2-822f-acaf28313dcf',
    // ★ Authority (機関): B2C テナントとユーザーフローを指定
    // 例: https://{your-tenant-name}.b2clogin.com/{your-tenant-name}.onmicrosoft.com/{your-signup-signin-flow-name}
    authority: 'https://moorii.b2clogin.com/moorii.onmicrosoft.com/B2C_1_signup_signin',
    // ★ Known Authorities (既知の機関): B2C のドメインを指定
    // 例: {your-tenant-name}.b2clogin.com
    knownAuthorities: ['moorii.b2clogin.com'],
    // ★ フロントエンド SPA で登録したリダイレクトURI
    redirectUri: 'http://localhost:5173', // またはあなたの開発環境のURL
    // (任意) ログアウト後のリダイレクト先
    // postLogoutRedirectUri: '/',
    // (任意) ネイティブアカウント (モバイルなど) での認証を有効にするか
    // navigateToLoginRequestUrl: true,
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
  // ★ バックエンド API で公開したスコープを指定 (完全な URI 形式)
  // 例: ["api://{your-backend-api-client-id}/API.Access"]
  scopes: ['https://moorii.onmicrosoft.com/7241feb0-196e-47aa-ae25-9399fd083b00/API.Access']
  
};