export interface ChatResponse {
  /**
   * AI によって生成された応答メッセージ
   */
  reply: string;

  // 将来的に他の情報 (例: 感情分析結果、トークン使用量など) が
  // 追加される可能性も考慮し、必要であればここに追記
  // emotion?: string;
  // usage?: { /* ... */ };
}
