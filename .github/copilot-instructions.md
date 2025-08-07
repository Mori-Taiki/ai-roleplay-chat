# AI ロールプレイチャット - Copilot 指示書

## アーキテクチャ概要

これは React TypeScript フロントエンドと ASP.NET Core バックエンドを持つフルスタック AI ロールプレイチャットアプリケーションで、Azure AD B2C 認証と Google Gemini AI および Replicate API サービスとの統合が特徴です。

### 主要コンポーネント
- **フロントエンド**: React 18 + TypeScript + Vite、MSAL による Azure AD B2C 認証
- **バックエンド**: ASP.NET Core 8 (C#)、Entity Framework Core with SQLite
- **AI サービス**: チャット用 Google Gemini、画像生成用 Replicate API
- **セキュリティ**: Azure Key Vault 統合による BYOK (Bring Your Own Key) パターン
- **デプロイ**: Azure Static Web Apps (フロントエンド) + App Service (バックエンド)

## 重要なパターン

### 認証・認可
- フロントエンドは `@azure/msal-react` と `useAuth` フックでトークン管理
- バックエンドは `BaseApiController` の `GetCurrentAppUserIdAsync()` でユーザーコンテキスト取得
- 全 API エンドポイントに `[Authorize]` が必須 - 保護されていないエンドポイントは作成しない
- BYOK パターン: ユーザーは `ApiKeyService` 経由で Azure Key Vault に独自の API キーを保存可能

### サービスアーキテクチャ
- サービスはインターフェースを実装 (例: `IGeminiService`, `IApiKeyService`)
- 全サービスは `Program.cs` の DI コンテナに登録
- BYOK サービスはユーザーキーを優先し、システムデフォルトにフォールバック
- 構造化ログには `ILogger` インジェクションを使用

### フロントエンドパターン
- API 呼び出し用カスタムフック: `useChatApi`, `useCharacterProfile` など
- `errorHandler.ts` ユーティリティによるエラーハンドリング
- コンポーネント構造: `pages/` (ルートコンポーネント), `components/` (再利用可能), `hooks/` (ロジック)
- React Router v6 と `AppLayout` コンポーネントで `<Outlet>` を使用した共有ナビゲーション

### データベース・エンティティ
- `Domain/Entities/` の EF Core エンティティと適切なリレーションシップ
- エンティティ関係: `User` → `CharacterProfile` → `ChatSession` → `ChatMessage`
- 開発環境は SQLite、マイグレーションは `Program.cs` で自動適用
- 全エンティティにマルチテナント分離用の `UserId` が必要

## 開発ワークフロー

### バックエンド開発
```bash
# /workspace/backend から実行
dotnet watch run              # 開発中のホットリロード
dotnet ef migrations add <name>  # 新しいマイグレーション作成
dotnet ef database update    # マイグレーション適用
```

### フロントエンド開発
```bash
# /workspace/frontend から実行
npm run dev                  # Vite 開発サーバー (ポート 5173)
npm run build               # プロダクションビルド
npm run lint                # ESLint チェック
```

### 設定管理
- バックエンド: `appsettings.json` + User Secrets (開発) + Azure App Settings (本番)
- フロントエンド: `.env.development` / `.env.production` と `VITE_` プレフィックス
- 秘密情報はコミットしない - User Secrets CLI を使用: `dotnet user-secrets set "Key" "Value"`

## AI サービス統合

### Gemini チャットフロー
1. `ChatController.PostAsync()` がチャットリクエストを受信
2. `ChatMessageService` 経由でチャット履歴を取得
3. BYOK パターンで `GeminiService.GenerateChatResponseAsync()` を呼び出し
4. AI がプロンプト分析で画像生成の必要性を判断
5. 必要な場合、`ReplicateService` で画像生成を呼び出し
6. `ChatMessageService.SaveChatMessageAsync()` でレスポンスを保存

### BYOK 実装
- ユーザーキー登録: `POST /api/ApiKey` に `{ "serviceName": "Gemini", "apiKey": "..." }` または `{ "serviceName": "Replicate", "apiKey": "..." }`
- サービスはユーザーキーを最初に確認し、システムデフォルトにフォールバック
- Key Vault 命名規則: `user-{userId}-{serviceName}-apikey`

## よくある落とし穴

### 認証の問題
- BYOK 機能では常に `userId` パラメータをサービスに渡す
- フロントエンド: API 呼び出し前に `isMsalLoading` をチェック
- 認証済みリクエストには `useAuth` フックの `acquireToken()` を使用

### Entity Framework
- 全エンティティに適切な分離用の `UserId` が必要
- コントローラーでは生のクレームではなく `GetCurrentAppUserIdAsync()` を使用
- カスケード削除設定済み: ユーザー削除で関連データも削除

### Azure デプロイ
- Static Web Apps 設定は `frontend/public/staticwebapp.config.json`
- SWA 設定経由でバックエンド App Service に API ルートをプロキシ
- 環境変数はコードではなく Azure ポータルで設定

## ファイル構成

変更時に理解すべき重要なファイル:
- `/backend/Program.cs` - DI 設定、ミドルウェア設定
- `/backend/Controllers/BaseApiController.cs` - 認証ヘルパー
- `/frontend/src/hooks/useAuth.ts` - トークン管理
- `/backend/Services/GeminiService.cs` - BYOK パターンの例
- `/backend/Services/ReplicateService.cs` - 画像生成サービス (IImagenService 実装)
- `/backend/Data/AppDbContext.cs` - EF エンティティ設定

新機能追加時の手順:
1. `/backend/Services/I{ServiceName}.cs` でインターフェース作成
2. 外部 API 使用時は BYOK パターンでサービス実装
3. `Program.cs` の DI コンテナに登録
4. `/frontend/src/hooks/` に対応するフロントエンドフック作成
5. `BaseApiController` を継承するコントローラー追加
