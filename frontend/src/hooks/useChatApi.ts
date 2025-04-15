// src/hooks/useChatApi.ts (新規作成)
import { useState, useCallback } from 'react';
import { getApiErrorMessage, getGenericErrorMessage } from '../utils/errorHandler';
import { ChatResponse } from '../models/ChatResponse';
import { ImageResponse } from '../models/ImageResponse';

const API_BASE_URL = 'https://localhost:7000'; // 環境変数推奨

interface UseChatApiReturn {
  isSendingMessage: boolean;
  isGeneratingImage: boolean;
  sendMessage: (
    characterId: number,
    prompt: string,
    sessionId: string | null,
    history?: { user: string; model: string }[]
  ) => Promise<ChatResponse | null>; // 履歴も渡せるように
  generateImage: (characterId: number, prompt: string) => Promise<ImageResponse | null>;
  error: string | null; // 共通のエラー状態
}

export const useChatApi = (): UseChatApiReturn => {
  const [isSendingMessage, setIsSendingMessage] = useState(false);
  const [isGeneratingImage, setIsGeneratingImage] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const sendMessage = useCallback(
    async (characterId: number, prompt: string, sessionId: string | null, history: any[] = []): Promise<ChatResponse | null> => {
      setIsSendingMessage(true);
      setError(null);
      try {
        // TODO: history の形式を ChatRequest に合わせる
        const requestBody = {
          Prompt: prompt,
          CharacterProfileId: characterId,
          SessionId: sessionId,
          History: history,
        };
        const response = await fetch(`${API_BASE_URL}/api/chat`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(requestBody),
        });
        if (!response.ok) {
          const message = await getApiErrorMessage(response);
          throw new Error(message);
        }
        const data: ChatResponse = await response.json();
        return data;
      } catch (err) {
        const message = getGenericErrorMessage(err, 'メッセージ送信');
        setError(message); // フック内でエラー状態を保持
        console.error('Send message error:', err);
        return null;
      } finally {
        setIsSendingMessage(false);
      }
    },
    []
  );

  const generateImage = useCallback(async (characterId: number, prompt: string): Promise<ImageResponse | null> => {
    setIsGeneratingImage(true);
    setError(null);
    try {
      // TODO: 画像生成 API が characterId を必要とするか確認
      const requestBody = { Prompt: prompt, CharacterProfileId: characterId }; // 必要なら CharacterProfileId を含める
      const response = await fetch(`${API_BASE_URL}/api/image/generate`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(requestBody),
      });
      if (!response.ok) {
        const message = await getApiErrorMessage(response);
        throw new Error(message);
      }
      const data: ImageResponse = await response.json(); // { mimeType, base64Data } を想定
      return data;
    } catch (err) {
      const message = getGenericErrorMessage(err, '画像生成');
      setError(message);
      console.error('Generate image error:', err);
      return null;
    } finally {
      setIsGeneratingImage(false);
    }
  }, []);

  return {
    isSendingMessage,
    isGeneratingImage,
    sendMessage,
    generateImage,
    error,
  };
};
