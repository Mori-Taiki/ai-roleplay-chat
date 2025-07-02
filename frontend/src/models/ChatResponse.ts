export interface ChatResponse {
  /**
   * AI によって生成された応答メッセージ
   */
  reply: string;
  sessionId: string;
  /**
   * 生成されたAIメッセージのDBにおけるID
   */
  aiMessageId: number;
  /**
   * この応答に対して画像を生成する必要があるかどうか
   */
  requiresImageGeneration: boolean;
}
