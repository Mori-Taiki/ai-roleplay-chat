import { useState, useCallback } from 'react';
import { useAuth } from './useAuth';
import { getApiErrorMessage, getGenericErrorMessage } from '../utils/errorHandler';

interface ApiKeyResponse {
  serviceName: string;
  hasKey: boolean;
}

interface UserApiKeysResponse {
  registeredServices: string[];
}

interface ApiKeyRequest {
  serviceName: string;
  apiKey: string;
}

interface UseApiKeysResult {
  registeredServices: string[];
  isLoading: boolean;
  error: string | null;
  
  // Operations
  getUserApiKeys: () => Promise<void>;
  registerApiKey: (serviceName: string, apiKey: string) => Promise<boolean>;
  checkApiKey: (serviceName: string) => Promise<boolean>;
  deleteApiKey: (serviceName: string) => Promise<boolean>;
  clearError: () => void;
}

export const useApiKeys = (): UseApiKeysResult => {
  const { acquireToken } = useAuth();
  const [registeredServices, setRegisteredServices] = useState<string[]>([]);
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

    const response = await fetch(url, {
      ...options,
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json',
        ...options.headers,
      },
    });

    return response;
  }, [acquireToken]);

  const getUserApiKeys = useCallback(async (): Promise<void> => {
    setIsLoading(true);
    setError(null);

    try {
      const response = await makeAuthenticatedRequest('/api/ApiKey');
      
      if (!response.ok) {
        const errorMessage = await getApiErrorMessage(response);
        throw new Error(errorMessage);
      }

      const data: UserApiKeysResponse = await response.json();
      setRegisteredServices(data.registeredServices);
    } catch (err) {
      const errorMessage = getGenericErrorMessage(err, 'APIキー情報の取得');
      setError(errorMessage);
      console.error('Failed to get user API keys:', err);
    } finally {
      setIsLoading(false);
    }
  }, [makeAuthenticatedRequest]);

  const registerApiKey = useCallback(async (
    serviceName: string, 
    apiKey: string
  ): Promise<boolean> => {
    setIsLoading(true);
    setError(null);

    try {
      const requestBody: ApiKeyRequest = { serviceName, apiKey };
      const response = await makeAuthenticatedRequest('/api/ApiKey', {
        method: 'POST',
        body: JSON.stringify(requestBody),
      });

      if (!response.ok) {
        const errorMessage = await getApiErrorMessage(response);
        throw new Error(errorMessage);
      }

      // Refresh the user's API keys after successful registration
      await getUserApiKeys();
      return true;
    } catch (err) {
      const errorMessage = getGenericErrorMessage(err, 'APIキーの登録');
      setError(errorMessage);
      console.error('Failed to register API key:', err);
      return false;
    } finally {
      setIsLoading(false);
    }
  }, [makeAuthenticatedRequest, getUserApiKeys]);

  const checkApiKey = useCallback(async (serviceName: string): Promise<boolean> => {
    try {
      const response = await makeAuthenticatedRequest(`/api/ApiKey/${encodeURIComponent(serviceName)}`);
      
      if (!response.ok) {
        const errorMessage = await getApiErrorMessage(response);
        throw new Error(errorMessage);
      }

      const data: ApiKeyResponse = await response.json();
      return data.hasKey;
    } catch (err) {
      console.error('Failed to check API key:', err);
      return false;
    }
  }, [makeAuthenticatedRequest]);

  const deleteApiKey = useCallback(async (serviceName: string): Promise<boolean> => {
    setIsLoading(true);
    setError(null);

    try {
      const response = await makeAuthenticatedRequest(`/api/ApiKey/${encodeURIComponent(serviceName)}`, {
        method: 'DELETE',
      });

      if (!response.ok) {
        const errorMessage = await getApiErrorMessage(response);
        throw new Error(errorMessage);
      }

      // Refresh the user's API keys after successful deletion
      await getUserApiKeys();
      return true;
    } catch (err) {
      const errorMessage = getGenericErrorMessage(err, 'APIキーの削除');
      setError(errorMessage);
      console.error('Failed to delete API key:', err);
      return false;
    } finally {
      setIsLoading(false);
    }
  }, [makeAuthenticatedRequest, getUserApiKeys]);

  return {
    registeredServices,
    isLoading,
    error,
    getUserApiKeys,
    registerApiKey,
    checkApiKey,
    deleteApiKey,
    clearError,
  };
};