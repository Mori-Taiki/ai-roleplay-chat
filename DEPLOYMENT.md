# デプロイメントガイド

このドキュメントは、AI ロールプレイチャットアプリケーションの自動デプロイメント設定について説明します。

## 概要

このアプリケーションは GitHub Actions を使用して Azure にデプロイされ、以下のアーキテクチャを採用しています：
- **フロントエンド**: Azure Static Web Apps
- **バックエンド**: Azure App Service (Linux)
- **データベース**: Azure Database for MySQL

## GitHub Actions ワークフロー

デプロイメントは `main` ブランチにコードがプッシュされたときに自動的にトリガーされます。ワークフローには3つのジョブが含まれています：

1. **フロントエンドデプロイメント** (`build_and_deploy_job`): React フロントエンドを Static Web Apps にデプロイ
2. **バックエンドデプロイメント** (`deploy_backend_job`): ASP.NET Core バックエンドを App Service にデプロイ
3. **PR クリーンアップ** (`close_pull_request_job`): PR がクローズされた際にプレビューデプロイメントをクリーンアップ

### フロントエンドビルドプロセス

フロントエンドデプロイメントでは、以下のステップが実行されます：

1. **Node.js セットアップ**: `frontend/.nvmrc` で指定されたバージョン（Node.js 20+）を使用
2. **依存関係インストール**: `npm ci` で依存関係をクリーンインストール
3. **型チェック**: `npm run typecheck` でTypeScriptの型エラーをチェック
4. **リント**: `npm run lint` でコード品質をチェック（エラーがあってもデプロイは継続）
5. **ビルドとデプロイ**: TypeScriptコンパイルとViteビルドを実行してAzure Static Web Appsにデプロイ

### ビルド要件

- **Node.js バージョン**: 20.0.0 以上（react-router-dom v7 の要件）
- **TypeScript**: 厳密な型チェックが有効（ビルド失敗時はデプロイ停止）
- **エラー処理**: TypeScript エラーがある場合はデプロイを中止し、成果物が正しくビルドされた状態でのみデプロイ実行

## 必要な GitHub シークレット

### 既存のシークレット（フロントエンド）
- `AZURE_STATIC_WEB_APPS_API_TOKEN_BLUE_PLANT_09D009000`: Static Web Apps デプロイメントトークン
- `PRODUCTION_API_URL`: バックエンド API ベース URL
- `PRODUCTION_B2C_CLIENT_ID`: Azure AD B2C クライアント ID
- `PRODUCTION_B2C_AUTHORITY`: Azure AD B2C オーソリティ URL
- `PRODUCTION_B2C_KNOWN_AUTHORITIES`: Azure AD B2C 既知オーソリティ
- `PRODUCTION_B2C_REDIRECT_URI`: Azure AD B2C リダイレクト URI
- `PRODUCTION_B2C_API_SCOPE_URI`: Azure AD B2C API スコープ URI

### 新規シークレット（バックエンド）
以下のシークレットを GitHub リポジトリに追加してください：

- `AZURE_APP_SERVICE_NAME`: Azure App Service の名前
- `AZURE_APP_SERVICE_PUBLISH_PROFILE`: Azure App Service から取得したパブリッシュプロファイル XML コンテンツ

## App Service デプロイメント設定

### 1. パブリッシュプロファイルの取得

1. Azure ポータルで Azure App Service に移動
2. 概要ブレードの **パブリッシュプロファイルの取得** をクリック
3. ダウンロードされた `.publishsettings` ファイルの全内容をコピー
4. GitHub で `AZURE_APP_SERVICE_PUBLISH_PROFILE` シークレットとして追加

#### パブリッシュプロファイルについて

パブリッシュプロファイル (`.publishsettings` ファイル) には以下の情報が含まれています：

**ファイル形式例:**
```xml
<?xml version="1.0" encoding="utf-8"?>
<publishData>
  <publishProfile 
    publishMethod="MSDeploy"
    publishUrl="your-app-name.scm.azurewebsites.net:443"
    msdeploysite="your-app-name"
    userName="$your-app-name"
    userPWD="xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
    destinationAppUrl="https://your-app-name.azurewebsites.net"
    SQLServerDBConnectionString=""
    mySQLDBConnectionString=""
    hostingProviderForumLink=""
    controlPanelLink="http://windows.azure.com"
    webSystem="WebSites">
  </publishProfile>
</publishData>
```

**重要な要素:**
- `publishUrl`: デプロイメント先のサーバー URL (通常は `*.scm.azurewebsites.net`)
- `msdeploysite`: App Service のサイト名
- `userName`: デプロイメント用ユーザー名 (通常は `$your-app-name` 形式)
- `userPWD`: デプロイメント用パスワード（自動生成された長いランダム文字列）

**GitHub シークレットへの設定方法:**
1. `.publishsettings` ファイルをテキストエディタで開く
2. **ファイル全体の内容**（XML全体）をコピー
3. GitHub の Repository Settings → Secrets and variables → Actions で `AZURE_APP_SERVICE_PUBLISH_PROFILE` として貼り付け
4. 改行や空白も含めて、XMLファイルの内容を完全にそのまま設定することが重要

### 2. App Service 名の設定

1. App Service 名を GitHub で `AZURE_APP_SERVICE_NAME` シークレットとして追加
2. これは Azure ポータルに表示される名前と一致する必要があります

### 3. App Service 設定の構成

App Service に以下のアプリケーション設定が構成されていることを確認してください：

#### 必須設定
- `AzureAdB2C:ClientId`: Azure AD B2C クライアント ID
- `AzureAdB2C:TenantId`: Azure AD B2C テナント ID
- `AzureAdB2C:ApiScopeUrl`: Azure AD B2C API スコープ URL
- `ConnectionStrings:DefaultConnection`: MySQL 接続文字列
- `GOOGLE_CLOUD_PROJECT`: Vertex AI 用 Google Cloud プロジェクト ID
- `KeyVault:VaultUri`: Azure Key Vault URI
- `FrontendUrl`: CORS 構成用フロントエンド URL

#### オプション設定
- `Gemini:ApiKey`: デフォルト Gemini API キー（フォールバック）
- `REPLICATE_API_TOKEN`: デフォルト Replicate API トークン（フォールバック）

## デプロイメントプロセス

1. **トリガー**: `main` ブランチへのプッシュまたは PR の作成/更新
2. **フロントエンド**: 常に Static Web Apps にデプロイ（PR プレビューを含む）
3. **バックエンド**: `main` ブランチプッシュ時のみ App Service にデプロイ
4. **クリーンアップ**: PR がクローズされた際にプレビューデプロイメントをクリーンアップ

## デプロイメント監視

1. GitHub Actions タブでワークフロー状況を確認
2. エラーがないかデプロイメントログを確認
3. デプロイメント後にアプリケーション機能を検証

## トラブルシューティング

### よくある問題

1. **フロントエンド TypeScript ビルド失敗**: 
   - TypeScript エラーがある場合、`npm run typecheck` でローカル確認
   - API 型定義とフロントエンド型の不一致を確認
   - 未使用変数や不正な型アクセスを修正
2. **Node.js バージョン不一致**: 
   - `frontend/.nvmrc` で Node.js 20+ が指定されていることを確認
   - react-router-dom v7 は Node.js 20+ が必須
3. **Oryx ビルド問題**: 
   - `frontend/.oryxrc` でプラットフォームバージョンが正しく指定されていることを確認
   - Azure Static Web Apps が正しい Node.js バージョンを使用していることを確認
4. **ビルド失敗**: .NET SDK バージョンの互換性を確認
5. **認証問題**: Azure AD B2C 設定を検証
6. **データベース接続**: 接続文字列が正しいことを確認
7. **API 呼び出し失敗**: CORS 設定とフロントエンド URL 構成を確認

### デバッグ手順

1. 具体的なエラーメッセージについて GitHub Actions ログを確認
2. すべての必要なシークレットが正しく設定されていることを確認
3. **フロントエンド TypeScript エラー**: 
   - ローカルで `npm run typecheck` を実行
   - ローカルで `npm run build` を実行してエラーを特定
4. **Node.js バージョン問題**: 
   - GitHub Actions ログで実際に使用された Node.js バージョンを確認
   - `frontend/.nvmrc` と `frontend/.oryxrc` の設定を確認
5. App Service 構成がローカル開発環境と一致することを確認
6. App Service からのデータベース接続をテスト

## 手動デプロイメント（緊急時）

自動デプロイメントが失敗した場合、手動でデプロイできます：

### バックエンド
```bash
cd backend
dotnet publish -c Release
# Azure ポータルまたは Azure CLI 経由で publish フォルダを App Service にアップロード
```

### フロントエンド
```bash
cd frontend
npm run build
# Azure ポータルまたは Azure CLI 経由で dist フォルダを Static Web Apps にデプロイ
```