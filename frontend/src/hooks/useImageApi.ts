import { useState, useCallback } from 'react';
import { getApiErrorMessage, getGenericErrorMessage } from '../utils/errorHandler';
import { useAuth } from './useAuth';

export interface ImageItem {
  messageId: number;
  characterId: number;
  sessionId: string;
  sessionTitle?: string;
  imageUrl: string;
  imagePrompt?: string;
  modelId?: string;
  serviceName?: string;
  createdAt: string;
}

export interface ImageGalleryResponse {
  items: ImageItem[];
  total: number;
  page: number;
  pageSize: number;
}

export interface SessionSummary {
  id: string;
  title: string;
  createdAt: string;
}

interface UseImageApiReturn {
  isLoading: boolean;
  isDeleting: boolean;
  images: ImageItem[];
  total: number;
  page: number;
  pageSize: number;
  sessions: SessionSummary[];
  getImages: (
    characterId: number,
    sessionId?: string,
    page?: number,
    pageSize?: number
  ) => Promise<ImageGalleryResponse | null>;
  deleteImage: (messageId: number) => Promise<boolean>;
  getSessionsByCharacter: (characterId: number) => Promise<SessionSummary[] | null>;
  error: string | null;
}

export const useImageApi = (): UseImageApiReturn => {
  const [isLoading, setIsLoading] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);
  const [images, setImages] = useState<ImageItem[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(40);
  const [sessions, setSessions] = useState<SessionSummary[]>([]);
  const [error, setError] = useState<string | null>(null);
  const { acquireToken } = useAuth();

  const getImages = useCallback(async (
    characterId: number,
    sessionId?: string,
    requestedPage: number = 1,
    requestedPageSize: number = 40
  ): Promise<ImageGalleryResponse | null> => {
    setIsLoading(true);
    setError(null);

    try {
      const token = await acquireToken();
      if (!token) {
        throw new Error('Authentication failed');
      }

      const url = new URL(`${import.meta.env.VITE_API_BASE_URL}/api/Image`);
      url.searchParams.append('characterId', characterId.toString());
      if (sessionId) {
        url.searchParams.append('sessionId', sessionId);
      }
      url.searchParams.append('page', requestedPage.toString());
      url.searchParams.append('pageSize', requestedPageSize.toString());

      const response = await fetch(url.toString(), {
        method: 'GET',
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json',
        },
      });

      if (!response.ok) {
        const errorMessage = await getApiErrorMessage(response);
        throw new Error(errorMessage);
      }

      const data: ImageGalleryResponse = await response.json();
      setImages(data.items);
      setTotal(data.total);
      setPage(data.page);
      setPageSize(data.pageSize);

      return data;
    } catch (err) {
      const errorMessage = getGenericErrorMessage(err);
      setError(errorMessage);
      return null;
    } finally {
      setIsLoading(false);
    }
  }, [acquireToken]);

  const deleteImage = useCallback(async (messageId: number): Promise<boolean> => {
    setIsDeleting(true);
    setError(null);

    try {
      const token = await acquireToken();
      if (!token) {
        throw new Error('Authentication failed');
      }

      const response = await fetch(`${import.meta.env.VITE_API_BASE_URL}/api/Image/${messageId}`, {
        method: 'DELETE',
        headers: {
          'Authorization': `Bearer ${token}`,
        },
      });

      if (!response.ok) {
        const errorMessage = await getApiErrorMessage(response);
        throw new Error(errorMessage);
      }

      // Remove the deleted image from local state
      setImages(prevImages => prevImages.filter(img => img.messageId !== messageId));
      setTotal(prevTotal => prevTotal - 1);

      return true;
    } catch (err) {
      const errorMessage = getGenericErrorMessage(err);
      setError(errorMessage);
      return false;
    } finally {
      setIsDeleting(false);
    }
  }, [acquireToken]);

  const getSessionsByCharacter = useCallback(async (characterId: number): Promise<SessionSummary[] | null> => {
    setError(null);

    try {
      const token = await acquireToken();
      if (!token) {
        throw new Error('Authentication failed');
      }

      const response = await fetch(`${import.meta.env.VITE_API_BASE_URL}/api/Sessions/character/${characterId}/summary`, {
        method: 'GET',
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json',
        },
      });

      if (!response.ok) {
        const errorMessage = await getApiErrorMessage(response);
        throw new Error(errorMessage);
      }

      const data: SessionSummary[] = await response.json();
      setSessions(data);

      return data;
    } catch (err) {
      const errorMessage = getGenericErrorMessage(err);
      setError(errorMessage);
      return null;
    }
  }, [acquireToken]);

  return {
    isLoading,
    isDeleting,
    images,
    total,
    page,
    pageSize,
    sessions,
    getImages,
    deleteImage,
    getSessionsByCharacter,
    error,
  };
};