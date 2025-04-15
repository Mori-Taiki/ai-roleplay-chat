import { useState, useCallback } from 'react';
import { getApiErrorMessage, getGenericErrorMessage } from '../utils/errorHandler';
import { ChatResponse } from '../models/ChatResponse';
import { ImageResponse } from '../models/ImageResponse';
import { Message } from '../models/Message';

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
  isLoadingHistory: boolean; // ★ 履歴読み込み中フラグ
  fetchHistory: (sessionId: string) => Promise<Message[] | null>; 
  isLoadingLatestSession: boolean; // ★ 追加
  fetchLatestSessionId: (characterId: number) => Promise<string | null>;
  error: string | null; // 共通のエラー状態
}

export const useChatApi = (): UseChatApiReturn => {
  const [isSendingMessage, setIsSendingMessage] = useState(false);
  const [isGeneratingImage, setIsGeneratingImage] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [isLoadingHistory, setIsLoadingHistory] = useState(false);
  const [isLoadingLatestSession, setIsLoadingLatestSession] = useState(false); 

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
  const fetchHistory = useCallback(async (sessionId: string): Promise<Message[] | null> => {
    setIsLoadingHistory(true);
    setError(null); 
    try {
      // ★ GET リクエストで履歴取得 API を呼び出す
      const response = await fetch(`${API_BASE_URL}/api/chat/history?sessionId=${encodeURIComponent(sessionId)}`);
      if (!response.ok) {
        const message = await getApiErrorMessage(response);
        throw new Error(message);
      }
      // ★ バックエンドの DTO に合わせた型で受け取る (id や timestamp の型変換が必要な場合がある)
      const historyData = await response.json();
      // ★ 必要ならフロントエンドの Message[] 型に変換
      const formattedHistory: Message[] = historyData.map((item: any) => ({
          id: item.id.toString(), // id を string に変換 (uuid じゃないので注意)
          sender: item.sender,
          text: item.text,
          imageUrl: item.imageUrl,
          // timestamp は必要なら Date オブジェクトに変換しても良い
      }));
      return formattedHistory;
    } catch (err) {
      const message = getGenericErrorMessage(err, 'チャット履歴の取得');
      setError(message);
      console.error("Fetch history error:", err);
      return null;
    } finally {
      setIsLoadingHistory(false);
    }
  }, []);

  const fetchLatestSessionId = useCallback(async (characterId: number): Promise<string | null> => {
    setIsLoadingLatestSession(true);
    setError(null);
    try {
      const response = await fetch(`${API_BASE_URL}/api/chat/sessions/latest?characterId=${characterId}`);
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
      console.error("Fetch latest session ID error:", err);
      return null;
    } finally {
      setIsLoadingLatestSession(false);
    }
  }, []);

  return {
    isSendingMessage,
    isGeneratingImage,
    sendMessage,
    generateImage,
    error,
    isLoadingHistory,
    fetchHistory,
    isLoadingLatestSession, 
    fetchLatestSessionId
  };
};
