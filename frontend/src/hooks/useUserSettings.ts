
import { useState, useCallback } from 'react';
import { useAuth } from './useAuth';
import { getApiErrorMessage, getGenericErrorMessage } from '../utils/errorHandler';

// このフックが扱う設定の型を定義
export interface UserSetting {
  serviceType: string;
  settingKey: string;
  settingValue: string;
}

interface UseUserSettingsReturn {
  settings: UserSetting[];
  isLoading: boolean;
  error: string | null;
  fetchUserSettings: () => Promise<void>;
  updateUserSettings: (settings: UserSetting[]) => Promise<boolean>;
  clearError: () => void;
}

export const useUserSettings = (): UseUserSettingsReturn => {
  const { acquireToken } = useAuth();
  const [settings, setSettings] = useState<UserSetting[]>([]);
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

  const fetchUserSettings = useCallback(async (): Promise<void> => {
    setIsLoading(true);
    setError(null);

    try {
      const response = await makeAuthenticatedRequest('/api/usersettings');
      
      if (!response.ok) {
        const errorMessage = await getApiErrorMessage(response);
        throw new Error(errorMessage);
      }

      const data: UserSetting[] = await response.json();
      setSettings(data);
    } catch (err) {
      const errorMessage = getGenericErrorMessage(err, 'ユーザー設定の取得');
      setError(errorMessage);
      console.error('Failed to get user settings:', err);
    } finally {
      setIsLoading(false);
    }
  }, [makeAuthenticatedRequest]);

  const updateUserSettings = useCallback(async (settingsToUpdate: UserSetting[]): Promise<boolean> => {
    setIsLoading(true);
    setError(null);

    try {
      const response = await makeAuthenticatedRequest('/api/usersettings', {
        method: 'PUT',
        body: JSON.stringify(settingsToUpdate),
      });

      if (!response.ok) {
        const errorMessage = await getApiErrorMessage(response);
        throw new Error(errorMessage);
      }

      // 更新成功後、最新の設定を再取得
      await fetchUserSettings();
      return true;
    } catch (err) {
      const errorMessage = getGenericErrorMessage(err, 'ユーザー設定の更新');
      setError(errorMessage);
      console.error('Failed to update user settings:', err);
      return false;
    } finally {
      setIsLoading(false);
    }
  }, [makeAuthenticatedRequest, fetchUserSettings]);

  return {
    settings,
    isLoading,
    error,
    fetchUserSettings,
    updateUserSettings,
    clearError,
  };
};
