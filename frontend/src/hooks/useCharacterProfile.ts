import { useState, useCallback } from 'react';
import { CharacterProfileResponse } from '../models/CharacterProfileResponse';
import { CreateCharacterProfileRequest } from '../models/CreateCharacterProfileRequest';
import { UpdateCharacterProfileRequest } from '../models/UpdateCharacterProfileRequest';
import { getApiErrorMessage, getGenericErrorMessage } from '../utils/errorHandler';
import { useAuth } from './useAuth';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'https://localhost:7000';

interface UseCharacterProfileReturn {
  character: CharacterProfileResponse | null;
  isLoading: boolean;
  error: string | null;
  isSubmitting: boolean;
  submitError: string | null;
  fetchCharacter: (id: number) => Promise<CharacterProfileResponse | null>;
  createCharacter: (data: CreateCharacterProfileRequest) => Promise<CharacterProfileResponse | null>;
  updateCharacter: (id: number, data: UpdateCharacterProfileRequest) => Promise<boolean>;
  deleteCharacter: (id: number) => Promise<boolean>;
}

export const useCharacterProfile = (): UseCharacterProfileReturn => {
  const [character, setCharacter] = useState<CharacterProfileResponse | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState<boolean>(false);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const { acquireToken } = useAuth();


  const fetchCharacter = useCallback(
    async (id: number): Promise<CharacterProfileResponse | null> => {
      const accessToken = await acquireToken();
      if (!accessToken) return null;

      setIsLoading(true);
      setError(null);
      try {
        const response = await fetch(`${API_BASE_URL}/api/characterprofiles/${id}`, {
          headers: { Authorization: `Bearer ${accessToken}` },
        });
        if (!response.ok) {
          const message = await getApiErrorMessage(response);
          throw new Error(message);
        }
        const data: CharacterProfileResponse = await response.json();
        setCharacter(data);
        return data;
      } catch (err) {
        const message = getGenericErrorMessage(err, 'キャラクターデータの読み込み');
        console.error('Error fetching character data:', err);
        setError(message);
        setCharacter(null);
        return null;
      } finally {
        setIsLoading(false);
      }
    },
    [acquireToken]
  );

  const createCharacter = useCallback(
    async (data: CreateCharacterProfileRequest): Promise<CharacterProfileResponse | null> => {
      const accessToken = await acquireToken();
      if (!accessToken) return null;

      setIsSubmitting(true);
      setSubmitError(null);
      try {
        const response = await fetch(`${API_BASE_URL}/api/characterprofiles`, {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            Authorization: `Bearer ${accessToken}`,
          },
          body: JSON.stringify(data),
        });
        if (!response.ok) {
          const message = await getApiErrorMessage(response);
          throw new Error(message);
        }
        const createdData: CharacterProfileResponse = await response.json();
        return createdData;
      } catch (err) {
        const message = getGenericErrorMessage(err, 'キャラクターの登録');
        console.error('Error creating character:', err);
        setSubmitError(message);
        return null;
      } finally {
        setIsSubmitting(false);
      }
    },
    [acquireToken]
  );

  const updateCharacter = useCallback(
    async (id: number, data: UpdateCharacterProfileRequest): Promise<boolean> => {
      const accessToken = await acquireToken();
      if (!accessToken) return false;

      setIsSubmitting(true);
      setSubmitError(null);
      try {
        const response = await fetch(`${API_BASE_URL}/api/characterprofiles/${id}`, {
          method: 'PUT',
          headers: {
            'Content-Type': 'application/json',
            Authorization: `Bearer ${accessToken}`,
          },
          body: JSON.stringify(data),
        });
        if (!response.ok) {
          const message = await getApiErrorMessage(response);
          throw new Error(message);
        }
        return true;
      } catch (err) {
        const message = getGenericErrorMessage(err, 'キャラクターの更新');
        console.error('Error updating character:', err);
        setSubmitError(message);
        return false;
      } finally {
        setIsSubmitting(false);
      }
    },
    [acquireToken]
  );

  const deleteCharacter = useCallback(
    async (id: number): Promise<boolean> => {
      const accessToken = await acquireToken();
      if (!accessToken) return false;

      setIsSubmitting(true);
      setSubmitError(null);
      try {
        const response = await fetch(`${API_BASE_URL}/api/characterprofiles/${id}`, {
          method: 'DELETE',
          headers: { Authorization: `Bearer ${accessToken}` },
        });
        if (!response.ok) {
          const message = await getApiErrorMessage(response);
          throw new Error(message);
        }
        return true;
      } catch (err) {
        const message = getGenericErrorMessage(err, 'キャラクターの削除');
        console.error('Error deleting character:', err);
        setSubmitError(message);
        return false;
      } finally {
        setIsSubmitting(false);
      }
    },
    [acquireToken]
  );

  return {
    character,
    isLoading,
    error,
    isSubmitting,
    submitError,
    fetchCharacter,
    createCharacter,
    updateCharacter,
    deleteCharacter,
  };
};
