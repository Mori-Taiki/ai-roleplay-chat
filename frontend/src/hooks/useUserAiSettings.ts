import { useState, useCallback } from 'react';
import { useAuth } from './useAuth';
import { AiGenerationSettingsRequest, AiGenerationSettingsResponse } from '../models/AiGenerationSettings';
import { getApiErrorMessage, getGenericErrorMessage } from '../utils/errorHandler';

interface UseUserAiSettingsReturn {
  settings: AiGenerationSettingsResponse | null;
  isLoading: boolean;
  error: string | null;
  fetchUserAiSettings: () => Promise<void>;
  updateUserAiSettings: (settings: AiGenerationSettingsRequest) => Promise<boolean>;
  deleteUserAiSettings: () => Promise<boolean>;
  clearError: () => void;
}

export const useUserAiSettings = (): UseUserAiSettingsReturn => {
  const { acquireToken } = useAuth();
  const [settings, setSettings] = useState<AiGenerationSettingsResponse | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const clearError = useCallback(() => {
    setError(null);
  }, []);

  const makeAuthenticatedRequest = useCallback(async (
    url: string,
    options: RequestInit = {}
  ): Promise<Response> => {
    const token = await acquireToken();
    if (!token) {
      throw new Error('認証トークンの取得に失敗しました');
    }

    const baseUrl = import.meta.env.VITE_API_URL || '';
    const response = await fetch(`${baseUrl}${url}`, {
      ...options,
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json',
        ...options.headers,
      },
    });

    return response;
  }, [acquireToken]);

  const fetchUserAiSettings = useCallback(async (): Promise<void> => {
    setIsLoading(true);
    setError(null);

    try {
      const response = await makeAuthenticatedRequest('/api/UserAiSettings');
      
      if (!response.ok) {
        const errorMessage = await getApiErrorMessage(response);
        throw new Error(errorMessage);
      }

      const data: AiGenerationSettingsResponse | null = await response.json();
      setSettings(data);
    } catch (err) {
      const errorMessage = getGenericErrorMessage(err, 'ユーザーAI設定の取得');
      setError(errorMessage);
      console.error('Failed to get user AI settings:', err);
    } finally {
      setIsLoading(false);
    }
  }, [makeAuthenticatedRequest]);

  const updateUserAiSettings = useCallback(async (settingsToUpdate: AiGenerationSettingsRequest): Promise<boolean> => {
    setIsLoading(true);
    setError(null);

    try {
      const response = await makeAuthenticatedRequest('/api/UserAiSettings', {
        method: 'PUT',
        body: JSON.stringify(settingsToUpdate),
      });

      if (!response.ok) {
        const errorMessage = await getApiErrorMessage(response);
        throw new Error(errorMessage);
      }

      const data: AiGenerationSettingsResponse = await response.json();
      setSettings(data);
      return true;
    } catch (err) {
      const errorMessage = getGenericErrorMessage(err, 'ユーザーAI設定の更新');
      setError(errorMessage);
      console.error('Failed to update user AI settings:', err);
      return false;
    } finally {
      setIsLoading(false);
    }
  }, [makeAuthenticatedRequest]);

  const deleteUserAiSettings = useCallback(async (): Promise<boolean> => {
    setIsLoading(true);
    setError(null);

    try {
      const response = await makeAuthenticatedRequest('/api/UserAiSettings', {
        method: 'DELETE',
      });

      if (!response.ok) {
        const errorMessage = await getApiErrorMessage(response);
        throw new Error(errorMessage);
      }

      setSettings(null);
      return true;
    } catch (err) {
      const errorMessage = getGenericErrorMessage(err, 'ユーザーAI設定の削除');
      setError(errorMessage);
      console.error('Failed to delete user AI settings:', err);
      return false;
    } finally {
      setIsLoading(false);
    }
  }, [makeAuthenticatedRequest]);

  return {
    settings,
    isLoading,
    error,
    fetchUserAiSettings,
    updateUserAiSettings,
    deleteUserAiSettings,
    clearError,
  };
};