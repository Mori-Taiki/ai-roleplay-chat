export interface ImageResponse {
  /**
   * 生成された画像の MIME タイプ (例: "image/png", "image/jpeg")
   */
  mimeType: string;

  /**
   * Base64 エンコードされた画像データ
   */
  base64Data: string;

  // 必要であれば他の情報 (例: プロンプト、生成に使用したモデル情報など) も追加
  // promptUsed?: string;
  // modelInfo?: string;
}
