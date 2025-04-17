# プロジェクト状況サマリー (2025-04-17)

## 1. プロジェクト基本情報

- **目的:** AIキャラクターとのリアルなコミュニケーション（ドキドキ、ときめき）を体験できるWebアプリケーションの開発。ユーザー定義キャラクター、文脈理解、自然な画像生成を重視。
- **形式:** Webアプリケーション (フロントエンド + バックエンド API)
- **リポジトリ等:** (必要であれば追記)

## 2. 技術スタックと構成

- **フロントエンド:** React (TypeScript, v18.x - B2C ライブラリ互換性のためダウングレード)、Vite, CSS (`index.css`, CSS Modules)。
    - ルーティング: `react-router-dom` (v6) 導入済、レイアウトコンポーネント (`AppLayout`, `Outlet`) 使用。
    - 状態管理: `useState`, `useReducer` (`ChatPage` でメッセージ管理に使用)。
    - API通信: カスタムフック (`useAuth`, `useChatApi`, `useCharacterProfile`, `useCharacterList`) による分離完了。`Workspace` API 使用。
    - UIコンポーネント: 共通コンポーネント `Button`, `FormField` 作成・適用済。`ChatPage` は `MessageList`, `MessageItem`, `ChatInput` に分割済。
    - 認証: `@azure/msal-browser`, `@azure/msal-react` を使用。`MsalProvider` でラップ、`AuthStatus` コンポーネントでログイン/ログアウト UI 実装済。
    - 環境変数: Vite の `.env` ファイル (`.env.development`, `.env.production`) と `import.meta.env.VITE_...` を使用。
    - フォルダ構成: `src/pages`, `src/components`, `src/models`, `src/hooks`, `src/utils`, `src/authConfig.ts` を使用。
- **バックエンド:** C# (ASP.NET Core, .NET 8 - 要確認) - Controller ベース。`BaseApiController` 導入。
    - `Controllers/`: `ChatController`, `ImageController`, `CharacterProfilesController` (UserId 連携修正済)。
    - `Services/`: `IGeminiService`, `GeminiService` (履歴対応済), `IImagenService`, `ImagenService`, `IUserService`, `UserService` (実装済), `IChatMessageService`, `ChatMessageService` (実装済、ChatController で利用)。
    - `Domain/Entities/`: `CharacterProfile`, `ChatMessage`, `ChatSession`, `User` (すべて実装、DB テーブル作成済)。
    * `Models/`: API DTO (`ChatRequest` (`SessionId?` 追加), `ChatResponse` (`SessionId`, `ImageUrl?` 追加), CRUD DTOs, `ChatMessageResponseDto`)。
    - データアクセス: Entity Framework Core (EF Core)。`AppDbContext` に全エンティティ DbSet 追加済。リポジトリパターンは未導入（将来タスク）。
    - 設定: `appsettings.json`, User Secrets (開発用機密情報), Azure App Service アプリケーション設定 (本番用)。
    - 認証: `Microsoft.Identity.Web` 使用。Azure AD B2C と連携。API は `[Authorize]` で保護済。
    - DI 活用。
- **文章生成:** Google Gemini API (`gemini-1.5-flash-latest`) を `GeminiService` 経由で利用。履歴連携、画像生成指示付きシステムプロンプト対応済。
- **画像生成:** Google Cloud Vertex AI (Imagen API, `imagegeneration@006`) を `ImagenService` 経由で利用。**認証は Azure Managed Identity + Workload Identity Federation (ADC) を使用** (設定完了、動作確認済)。
- **データベース:** MySQL (Azure Database for MySQL フレキシブルサーバー利用)。
    - ORM: EF Core 使用。`ChatMessages`, `ChatSessions`, `Users` テーブル追加済み。リレーションシップ (Cascade Delete 含む) 設定済。`ChatMessages.ImageUrl` は `MEDIUMTEXT` に変更済。
- **クラウド:**
    - Azure: DBaaS (MySQL) 利用中。**App Service (Linux)** と **Static Web Apps** リソース作成済。Azure AD B2C 利用中。
    - Google Cloud: Gemini API, Vertex AI Imagen 利用中。Workload Identity 連携設定済。
- **開発環境:** VSCode, Git, .NET SDK, Node.js/npm(or yarn)。

## 3. 主要な決定事項・経緯

- **コアコンセプト:** 変更なし。
- **リファクタリング:** フロントエンド・バックエンドともにカスタムフック/サービス導入、DI、認証連携など大幅に進捗。アーキテクチャのレイヤード化 (Repository パターン) は将来タスクに。
- **画像生成トリガー:** ユーザーボタンから **AI (Gemini) による文脈判断** に方針変更。バックエンド実装完了。
- **認証:** Azure AD B2C を採用、実装完了。バックエンドの GCP 認証は Managed Identity + WIF に変更・実装完了。
- **設定管理:** User Secrets, `.env` ファイル, Azure App Settings/SWA App Settings を導入。
- **デプロイ:** Azure (SWA + App Service + MySQL) に決定。リソース作成完了。環境設定完了。**アプリケーションのデプロイ自体も完了し、認証・基本機能が動作確認済。**

## 4. 現状のコード概要 (バックエンド)

- `Program.cs`: DI (Services, Repositories未, DbContext), 認証/認可ミドルウェア, CORS 設定 (環境変数読み込み), Swagger (認証対応済), 環境変数設定 (B2C, Vertex AI ADC 用 `GOOGLE_CLOUD_PROJECT` など)。
- `Controllers/`: `BaseApiController` (ユーザーID取得共通化)。`CharacterProfilesController`, `ChatController` は `BaseApiController` を継承し、`UserId` を利用したデータアクセスに修正済。`ImageController` は `ChatMessageService` 利用未実装。
- `Services/`: `UserService`, `ChatMessageService` 追加・実装済。`GeminiService` は履歴とシステムプロンプト指示に対応済。`ImagenService` は Managed Identity 認証で動作。
- `Domain/Entities/`: `CharacterProfile`, `ChatMessage`, `ChatSession`, `User` 実装済。`CharacterProfile.SystemPromptHelper` 追加。
- `Models/`: `ChatRequest`, `ChatResponse` 修正済。DTO 各種。
- `Data/`: `AppDbContext` に全エンティティ追加、リレーションシップ設定済。

## 5. 現状のコード概要 (フロントエンド)

- `main.tsx`: `MsalProvider` でラップ。
- `App.tsx`: `AppLayout` コンポーネント導入、共通ナビゲーション、`AuthStatus` 配置、`Outlet` 使用。
- `pages/ChatPage.tsx`: `useReducer` でメッセージ管理。カスタムフック `useChatApi` `useCharacterProfile` 利用。履歴表示、AI判断による画像表示対応済。**キャラクター名表示実装済**。セッション ID 管理・連携実装済。
- `pages/CharacterListPage.tsx`: カスタムフック `useCharacterList` 利用。`Button` コンポーネント適用。「会話する」ボタン実装済。
- `pages/CharacterSetupPage.tsx`: `react-hook-form`, `useFieldArray` 利用。カスタムフック `useCharacterProfile` 利用。`FormField`, `Button` コンポーネント適用。「会話する」ボタン実装済。
- `components/`: `AuthStatus`, `Button`, `FormField`, `MessageList`, `MessageItem`, `ChatInput` 作成・利用。
- `hooks/`: `useAuth`, `useChatApi`, `useCharacterProfile`, `useCharacterList` 作成・利用。API 呼び出し時にアクセストークン付与実装済。
- `models/`: 型定義各種。`ChatResponse` に `ImageUrl?` 追加。
- `utils/`: `errorHandler.ts` 作成・利用。
- `authConfig.ts`: MSAL 設定。環境変数 (`import.meta.env.VITE_...`) から値を読み込むように修正済。
- `.env.development`, `.env.production`: 作成・設定済。`.gitignore` 設定済。

## 6. プロジェクトフォルダ構成 (主要部分)

将来的なリファクタリングは検討中

backend/
├── Controllers/
│   ├── BaseApiController.cs (New)
│   ├── ChatController.cs
│   ├── ImageController.cs
│   └── CharacterProfilesController.cs
├── Data/
│   ├── Migrations/
│   └── AppDbContext.cs
├── Domain/
│   └── Entities/
│       ├── CharacterProfile.cs
│       ├── ChatMessage.cs (New)
│       ├── ChatSession.cs (New)
│       └── User.cs (New)
├── Models/
│   ├── ChatMessageResponseDto.cs (New)
│   ├── ChatRequest.cs (Updated)
│   ├── ChatResponse.cs (Updated)
│   ├── CreateCharacterProfileRequest.cs
│   ├── UpdateCharacterProfileRequest.cs
│   └── ...
├── Services/
│   ├── IChatMessageService.cs (New)
│   ├── ChatMessageService.cs (New)
│   ├── IGeminiService.cs
│   ├── GeminiService.cs (Updated)
│   ├── IImagenService.cs
│   ├── ImagenService.cs
│   ├── IUserService.cs (New)
│   └── UserService.cs (New)
├── appsettings.json
├── Program.cs (Updated)
└── backend.csproj
frontend/
├── public/
├── src/
│   ├── components/
│   │   ├── AuthStatus.tsx (New)
│   │   ├── Button.tsx (New)
│   │   ├── ChatInput.tsx (New)
│   │   ├── FormField.tsx (New)
│   │   ├── MessageItem.tsx (New)
│   │   └── MessageList.tsx (New)
│   ├── hooks/ (New)
│   │   ├── useAuth.ts
│   │   ├── useChatApi.ts
│   │   ├── useCharacterList.ts
│   │   └── useCharacterProfile.ts
│   ├── models/
│   │   ├── CharacterProfileResponse.ts
│   │   ├── ChatResponse.ts (New/Updated)
│   │   ├── CreateCharacterProfileRequest.ts
│   │   └── UpdateCharacterProfileRequest.ts
│   ├── pages/
│   │   ├── CharacterListPage.tsx (Updated)
│   │   ├── CharacterSetupPage.tsx (Updated)
│   │   └── ChatPage.tsx (Updated)
│   ├── utils/ (New)
│   │   └── errorHandler.ts
│   ├── App.css / index.css
│   ├── App.tsx (Updated)
│   ├── authConfig.ts (New)
│   ├── main.tsx (Updated)
│   └── ...
├── .env.development (New)
├── .env.production (New)
├── .gitignore (Updated)
├── index.html
├── package.json
├── tsconfig.json
└── vite.config.ts

## 7. ToDoリスト (状況とネクストアクション - 2025-04-17)

### 完了済みタスク
- 環境準備・調査 (GCP Vertex AI - 基本)
- 外部API疎通確認 (Gemini)
- 基本的なWebアプリフロー構築 (フロント-バックエンド-Gemini)
- バックエンドリファクタリング (モデル分離, 設定外部化, Service層導入 (一部), Controller移行, BaseApiController)
- 画像生成機能 (基本): Imagen連携, 日本語→英語翻訳(Gemini), フロント表示(ボタン)
- DB連携設定: ORM(EF Core), Azure DB作成/設定, パッケージ導入, DbContext骨子作成/DI登録, 接続確認, マイグレーション初期設定
- **DBスキーマ:** `CharacterProfiles`, `ChatMessages`, `ChatSessions`, `Users` テーブル作成、リレーション設定、`ImageUrl` 型変更
- **タスク⑤: キャラクター設定 (基礎):** CRUD API 実装、フロント連携 (`CharacterSetupPage`)
- **フロントエンド: リファクタリング** (ルーティング, カスタムフック, 共通コンポーネント, `ChatPage` 分割, `react-hook-form`, `useFieldArray`)
- **タスク⑦: トーク画面 (`ChatPage.tsx`) 修正とリファクタリング:** ID連携、API通信分離、状態管理改善(`useReducer`, `uuid`)、コンポーネント分割、**キャラクター名表示**
- **タスク⑧: ID 認証機能の実装 (Azure AD B2C):** Azure/GCP(WIF)/バックエンド/フロントエンド設定、**DB連携 (UserId利用)**
- **デプロイ準備:** 環境選定(SWA+AppService+MySQL)、Azureリソース作成、環境変数管理(UserSecrets, .env, App Settings)、**Managed Identity認証 (Vertex AI)**
- **デプロイ:** フロントエンド(SWA), バックエンド(App Service) の初回デプロイ、**動作確認 (ログイン、基本API呼び出し成功)**
* **チャット履歴機能 (フェーズ1):** 基本的な保存・表示・文脈利用 (Gemini連携含む)
* **チャットメッセージ保存処理の共通化 (一部):** Service 作成・DI登録、`ChatController` での利用完了。
* **AIによる画像生成判定化 (バックエンド):** `GeminiService` プロンプト修正、`ChatController` での判定・Imagen呼び出し・メッセージ保存・レスポンス返却実装完了。

### 現在のタスク / 次のステップ

* [ ] **【要調査/修正】App Service から Azure IMDS (169.254.169.254) への接続拒否 (Connection refused) エラー解消**
    * **状況:** バックエンド (App Service) 起動時、Vertex AI クライアント生成中に ADC が Azure Managed Identity トークンを取得しようとして IMDS (`169.254.169.254:80`) にアクセスするが、「Connection refused」となり失敗。結果として `Your default credentials were not found` 例外が発生し、アプリケーションが起動できない（または Imagen 呼び出し時に失敗する）。
    * **原因調査:** App Service のネットワーク設定 (VNet統合、NSG、ASEなど) が IMDS へのアクセスを妨げていないか確認。プラットフォームの一時的な問題の可能性も考慮。
    * **ゴール:** App Service コンソールからの `curl` テストで IMDS に接続でき、バックエンド起動時の認証情報エラーが解消されること。
* [ ] **チャットメッセージ保存処理の共通化 (仕上げ)** ← **Next Action?**
    * [ ] `ImageController` (または画像生成箇所) で `ChatMessageService` を利用して画像メッセージを保存するように修正する。
* [ ] **生成画像の再表示問題の解決:** 上記共通化により解決される見込み。表示CSSの最終調整。
* [ ] **セッション管理 UI:** 画面から過去のチャットセッションを選択・削除する機能。
* [ ] **履歴要約機能 (フェーズ 2):** 長い会話履歴の要約と利用。

### 次のフェーズ以降のタスク (優先度順未定)
- [ ] 機能改善: 画像生成 (エラー理由表示、複数対応、表示改善など)
- [ ] 機能改善: ログ・エラー処理 (全体改善、構造化ロギング検討)
- [ ] UI/UX改善 (全体デザイン、レスポンシブ、UIライブラリ検討)
- [ ] セキュリティ考慮 (入力検証強化など)
- [ ] デプロイ強化 (CI/CD パイプライン構築、カスタムドメイン、HTTPS)
- [ ] コスト試算・監視
* [ ] **アーキテクチャリファクタリング:** レイヤード化、Repository パターン導入 (Deferred)
- [ ] 技術調査: Azure ML / Stable Diffusion
- [ ] 新機能アイデア: キャラクタータイムライン機能

## 8. PCスペック (参考)
* CPU: i7-9700KF, RAM: 16GB, GPU: GT 730, OS: Win10 Home.
