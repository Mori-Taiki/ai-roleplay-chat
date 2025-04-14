// src/hooks/useCharacterList.ts (新規作成)
import { useState, useEffect, useCallback } from 'react';
import { CharacterProfileResponse } from '../models/CharacterProfileResponse';
import { getApiErrorMessage, getGenericErrorMessage } from '../utils/errorHandler';

const API_BASE_URL = 'https://localhost:7000';

interface UseCharacterListReturn {
  characters: CharacterProfileResponse[];
  isLoading: boolean;
  error: string | null;
  fetchCharacters: () => void; // 再取得用の関数も用意しておくと便利
}

export const useCharacterList = (): UseCharacterListReturn => {
  const [characters, setCharacters] = useState<CharacterProfileResponse[]>([]);
  const [isLoading, setIsLoading] = useState<boolean>(true); // 初期状態は true
  const [error, setError] = useState<string | null>(null);

  const fetchCharacters = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    try {
      const response = await fetch(`${API_BASE_URL}/api/characterprofiles`);
      if (!response.ok) {
        const message = await getApiErrorMessage(response);
        throw new Error(message);
      }
      const data: CharacterProfileResponse[] = await response.json();
      setCharacters(data);
    } catch (err) {
      const message = getGenericErrorMessage(err, 'キャラクターリストの取得');
      setError(message);
      console.error('Error fetching characters:', err);
      setCharacters([]); // エラー時は空にする
    } finally {
      setIsLoading(false);
    }
  }, []); // useCallback の依存配列は空

  // コンポーネントマウント時に自動的に取得開始
  useEffect(() => {
    fetchCharacters();
  }, [fetchCharacters]); // fetchCharacters が変更された場合 (通常は初回のみ)

  return { characters, isLoading, error, fetchCharacters };
};
