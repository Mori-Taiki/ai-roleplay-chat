# Geminiエージェント開発ガイドライン

## 1. あなたの役割と基本原則

あなたは、このプロジェクトの開発を支援するAIエージェントです。あなたの主な目標は、ユーザーの指示に基づき、既存のアーキテクチャと規約を厳守しながら、安全かつ効率的にコードの変更、機能追加、デバッグを行うことです。

### **基本原則**
- **規約の厳守:** 新しいコードを追加・変更する際は、必ず既存のコードのスタイル、命名規則、アーキテクチャパターンに従ってください。
- **ツールの活用:** ファイルの検索には `glob`、内容の読解には `read_file`、複数ファイルの読解には `read_many_files`、コマンド実行には `run_shell_command` を積極的に使用してください。
- **確認の徹底:** 不明な点や、大きな変更を行う前には、必ずユーザーに確認してください。
- **セキュリティ最優先:** 認証・認可の仕組みを迂回したり、機密情報をハードコーディングしたりすることは **NEVER** 行わないでください。

---

## 2. アーキテクチャ概要

このアプリケーションは、React/TypeScriptフロントエンドとASP.NET Coreバックエンドで構成されています。

- **フロントエンド**: React 18, TypeScript, Vite, MSAL (認証)
- **バックエンド**: ASP.NET Core 8 (C#), Entity Framework Core, SQLite (開発用)
- **AIサービス**: Google Gemini (チャット), Replicate API (画像生成)
- **セキュリティ**: Azure Key Vault を利用した BYOK (Bring Your Own Key) パターン
- **デプロイ**: Azure Static Web Apps (フロントエンド) + App Service (バックエンド)

---

## 3. 守るべき重要なルール

### 認証・認可 (MUST)
- **フロントエンド:**
    - 認証トークンの管理には `frontend/src/hooks/useAuth.ts` の `useAuth` フックを使用します。
    - 認証が必要なAPIを呼び出す前には、必ず `acquireToken()` を呼び出してアクセストークンを取得してください。
    - API呼び出し前には `isMsalLoading` をチェックし、MSALの処理が完了していることを確認してください。
- **バックエンド:**
    - ユーザーコンテキストの取得には、`BaseApiController` が提供する `GetCurrentAppUserIdAsync()` を使用してください。生のクレームは **NEVER** 使用しないでください。
    - 新しいAPIエンドポイントには、必ず `[Authorize]` 属性を付与してください。
- **BYOK (Bring Your Own Key):**
    - ユーザー固有のAPIキーを使用するサービスを呼び出す際は、必ず `userId` を引数として渡してください。
    - キーの保存・取得・削除は `ApiKeyService` を介して行います。サービスはユーザーキーを優先し、なければシステムデフォルトキーにフォールバックします。
    - Key Vaultのシークレット名は `user-{userId}-{serviceName}-apikey` の規約に従ってください。

### データベース (MUST)
- 全てのエンティティは、マルチテナント分離のため `UserId` を持つ必要があります。
- エンティティ間のリレーションシップは `backend/src/Data/AppDbContext.cs` で定義されています。カスケード削除が設定されているため、`User` を削除すると関連データも削除されます。

### 設定管理 (MUST)
- 機密情報（APIキー、接続文字列など）は、**NEVER** ソースコードに直接記述しないでください。
- **バックエンド:** 開発環境では User Secrets (`dotnet user-secrets set "Key" "Value"`) を使用します。
- **フロントエンド:** 環境変数は `.env` ファイルに `VITE_` プレフィックスを付けて定義します。

---

## 4. 開発ワークフロー

### コンテキストの理解
タスクを開始する前に、以下の主要ファイルを `read_file` または `read_many_files` で読み込み、プロジェクトの構造とパターンを理解してください。
- `/backend/Program.cs` (DI設定、ミドルウェア)
- `/backend/Controllers/BaseApiController.cs` (認証ヘルパー)
- `/frontend/src/hooks/useAuth.ts` (トークン管理)
- `/backend/Services/GeminiService.cs` (BYOKパターンの実装例)
- `/backend/Data/AppDbContext.cs` (EFエンティティ設定)

### バックエンド開発
`/workspace/backend` ディレクトリで以下のコマンドを `run_shell_command` で実行します。
- **ホットリロード:** `dotnet watch run`
- **DBマイグレーション作成:** `dotnet ef migrations add <MigrationName>`
- **DB更新:** `dotnet ef database update`

### フロントエンド開発
`/workspace/frontend` ディレクトリで以下のコマンドを `run_shell_command` で実行します。
- **開発サーバー起動:** `npm run dev`
- **プロダクションビルド:** `npm run build`
- **Lintチェック:** `npm run lint`

---

## 5. 標準的な実装フロー

### 新機能追加時の手順
1.  **インターフェース定義:** `/backend/Services/I{ServiceName}.cs` を `write_file` で作成します。
2.  **サービス実装:** 外部APIを使用する場合は、BYOKパターンに従ってサービスクラスを実装します。
3.  **DI登録:** `backend/Program.cs` を `replace` または `write_file` で編集し、新しいサービスをDIコンテナに登録します。
4.  **コントローラー作成:** `BaseApiController` を継承してコントローラーを追加します。
5.  **フロントエンドフック作成:** `/frontend/src/hooks/` に、バックエンドAPIを呼び出すためのカスタムフックを `write_file` で作成します。

### Gemini チャットフロー
1.  `ChatController.PostAsync()` がリクエストを受信します。
2.  `ChatMessageService` を使用して過去のチャット履歴を取得します。
3.  `GeminiService.GenerateChatResponseAsync()` を呼び出します。この際、`userId` を渡してBYOKパターンを適用します。
4.  レスポンスに画像生成が必要かどうかの判断が含まれます。
5.  必要に応じて `ReplicateService` を呼び出します。
6.  `ChatMessageService.SaveChatMessageAsync()` を使用して会話を保存します。