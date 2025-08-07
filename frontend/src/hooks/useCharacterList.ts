// src/hooks/useCharacterList.ts (新規作成)
import { useState, useEffect, useCallback } from 'react';
import { useIsAuthenticated } from '@azure/msal-react';
import { CharacterProfileWithSessionInfoResponse } from '../models/CharacterProfileResponse';
import { getApiErrorMessage, getGenericErrorMessage } from '../utils/errorHandler';
import { useAuth } from './useAuth';

interface UseCharacterListReturn {
  characters: CharacterProfileWithSessionInfoResponse[];
  isLoading: boolean;
  error: string | null;
  fetchCharacters: () => void; // 再取得用の関数も用意しておくと便利
}

export const useCharacterList = (): UseCharacterListReturn => {
  const [characters, setCharacters] = useState<CharacterProfileWithSessionInfoResponse[]>([]);
  const isAuthenticated = useIsAuthenticated();
  const [isLoading, setIsLoading] = useState<boolean>(true); // 初期状態は true
  const [error, setError] = useState<string | null>(null);
  const { acquireToken } = useAuth();

  const fetchCharacters = useCallback(async () => {
    if (!isAuthenticated) {
      setIsLoading(false);
      setCharacters([]);
      setError(null);
      return;
    }
    setIsLoading(true);
    setError(null);

    const accessToken = await acquireToken();
    if (!accessToken) return null;

    setIsLoading(true);
    setError(null);
    try {
      const baseUrl = import.meta.env.VITE_API_URL || '';
      // ★ GET /api/characterprofiles を呼び出す
      const response = await fetch(`${baseUrl}/api/characterprofiles`, {
        headers: { Authorization: `Bearer ${accessToken}` },
      });
      if (!response.ok) {
        // ... (APIエラーハンドリング)
        const message = await getApiErrorMessage(response);
        setError(message);
        throw new Error(message || `API Error: ${response.status}`);
      }
      // ★ 更新された型でレスポンスをパース
      const data: CharacterProfileWithSessionInfoResponse[] = await response.json();
      setCharacters(data);
    } catch (err) {
      const message = getGenericErrorMessage(err, 'キャラクターリストの取得');
      setError(message);
      console.error('Error fetching characters:', err);
      setCharacters([]); // エラー時は空にする
    } finally {
      setIsLoading(false);
    }
  }, [acquireToken]);

  useEffect(() => {
    if (isAuthenticated) {
      fetchCharacters();
    } else {
      setCharacters([]);
      setError(null);
      setIsLoading(false);
    }
  }, [isAuthenticated, fetchCharacters]);

  return { characters, isLoading, error, fetchCharacters };
};