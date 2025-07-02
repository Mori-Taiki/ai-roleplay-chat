import { useState, useCallback } from 'react';
import { getApiErrorMessage, getGenericErrorMessage } from '../utils/errorHandler';
import { ChatResponse } from '../models/ChatResponse';
import { Message } from '../models/Message';
import { useAuth } from './useAuth';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'https://localhost:7000'; // 環境変数推奨

interface UseChatApiReturn {
  isSendingMessage: boolean;
  isGeneratingImage: boolean;
  sendMessage: (
    characterId: number,
    prompt: string,
    sessionId: string | null
  ) => Promise<ChatResponse | null>; // ★ 戻り値の型を更新
  generateAndUploadImage: (messageId: number) => Promise<ImageUploadResponse | null>; 
  isLoadingHistory: boolean;
  fetchHistory: (sessionId: string) => Promise<Message[] | null>;
  isLoadingLatestSession: boolean;
  fetchLatestSessionId: (characterId: number) => Promise<string | null>;
  error: string | null;
}
interface ImageUploadResponse {
  imageUrl: string;
}

export const useChatApi = (): UseChatApiReturn => {
  const [isSendingMessage, setIsSendingMessage] = useState(false);
  const [isGeneratingImage, setIsGeneratingImage] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [isLoadingHistory, setIsLoadingHistory] = useState(false);
  const [isLoadingLatestSession, setIsLoadingLatestSession] = useState(false);
  const { acquireToken } = useAuth();

  const sendMessage = useCallback(
    async (
      characterId: number,
      prompt: string,
      sessionId: string | null,
    ): Promise<ChatResponse | null> => {
      const accessToken = await acquireToken(); 
      if (!accessToken) return null; // ★ トークンなければ中断

      setIsSendingMessage(true);
      setError(null);
      try {
        const requestBody = {
          Prompt: prompt,
          CharacterProfileId: characterId,
          SessionId: sessionId,
        };
        const response = await fetch(`${API_BASE_URL}/api/chat`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${accessToken}` },
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
        setError(message);
        console.error('Send message error:', err);
        return null;
      } finally {
        setIsSendingMessage(false);
      }
    },
    [acquireToken]
  );

  const generateAndUploadImage = useCallback(
    async (messageId: number): Promise<ImageUploadResponse | null> => {
      const accessToken = await acquireToken(); 
      if (!accessToken) return null;

      setIsGeneratingImage(true);
      setError(null);
      try {
        const requestBody = { MessageId: messageId };
        // ★ 新しいエンドポイントを呼び出す
        const response = await fetch(`${API_BASE_URL}/api/image/generate-and-upload`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${accessToken}` },
          body: JSON.stringify(requestBody),
        });
        if (!response.ok) {
          const message = await getApiErrorMessage(response);
          throw new Error(message);
        }
        const data: ImageUploadResponse = await response.json(); // { imageUrl } を想定
        return data;
      } catch (err) {
        const message = getGenericErrorMessage(err, '画像生成');
        setError(message);
        console.error('Generate image error:', err);
        return null;
      } finally {
        setIsGeneratingImage(false);
      }
    },
    [acquireToken]
  );

  const fetchHistory = useCallback(
    async (sessionId: string): Promise<Message[] | null> => {
      const accessToken = await acquireToken();
      if (!accessToken) return null;

      setIsLoadingHistory(true);
      setError(null);
      try {
        const response = await fetch(`${API_BASE_URL}/api/chat/history?sessionId=${encodeURIComponent(sessionId)}`, {
          headers: {
            Authorization: `Bearer ${accessToken}`,
          },
        });

        if (!response.ok) {
          const message = await getApiErrorMessage(response);
          throw new Error(message);
        }
        const historyData = await response.json();
        const formattedHistory: Message[] = historyData.map((item: any) => ({
          id: item.id.toString(),
          sender: item.sender,
          text: item.text,
          imageUrl: item.imageUrl,
        }));
        return formattedHistory;
      } catch (err) {
        const message = getGenericErrorMessage(err, 'チャット履歴の取得');
        setError(message);
        console.error('Fetch history error:', err);
        return null;
      } finally {
        setIsLoadingHistory(false);
      }
    },
    [acquireToken, setError]
  );

  const fetchLatestSessionId = useCallback(
    async (characterId: number): Promise<string | null> => {
      const accessToken = await acquireToken();
      if (!accessToken) return null; // ★ トークンなければ中断

      setIsLoadingLatestSession(true);
      setError(null);
      try {
        const response = await fetch(`${API_BASE_URL}/api/chat/sessions/latest?characterId=${characterId}`, {
          headers: {
            Authorization: `Bearer ${accessToken}`, // ★ ヘッダーに追加
          },
        });
        if (!response.ok) {
          if (response.status === 404) {
            // 404 はアクティブセッションがない場合なので、エラーではなく null を返す
            console.log(`No active session found for character ${characterId}`);
            return null;
          }
          const message = await getApiErrorMessage(response);
          throw new Error(message);
        }
        const sessionId: string = await response.text(); // UUID文字列を直接受け取る想定
        return sessionId;
      } catch (err) {
        const message = getGenericErrorMessage(err, '最新セッションIDの取得');
        setError(message);
        console.error('Fetch latest session ID error:', err);
        return null;
      } finally {
        setIsLoadingLatestSession(false);
      }
    },
    [acquireToken, setError]
  );

  return {
    isSendingMessage,
    isGeneratingImage,
    sendMessage,
    generateAndUploadImage,
    error,
    isLoadingHistory,
    fetchHistory,
    isLoadingLatestSession,
    fetchLatestSessionId,
  };
};
