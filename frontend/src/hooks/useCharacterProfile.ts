import { useState, useCallback } from "react";
import { CharacterProfileResponse } from "../models/CharacterProfileResponse";
import { CreateCharacterProfileRequest } from "../models/CreateCharacterProfileRequest";
import { UpdateCharacterProfileRequest } from "../models/UpdateCharacterProfileRequest";
import {
  getApiErrorMessage,
  getGenericErrorMessage,
} from "../utils/errorHandler"; // インポート

const API_BASE_URL = "https://localhost:7000/api/characterprofiles"; // 環境変数などに移動推奨

interface UseCharacterProfileReturn {
  character: CharacterProfileResponse | null;
  isLoading: boolean;
  error: string | null;
  isSubmitting: boolean;
  submitError: string | null;
  fetchCharacter: (id: number) => Promise<CharacterProfileResponse | null>;
  createCharacter: (
    data: CreateCharacterProfileRequest
  ) => Promise<CharacterProfileResponse | null>;
  updateCharacter: (
    id: number,
    data: UpdateCharacterProfileRequest
  ) => Promise<boolean>; // 成功/失敗を返す
  deleteCharacter: (id: number) => Promise<boolean>; // 成功/失敗を返す
}

export const useCharacterProfile = (): UseCharacterProfileReturn => {
  const [character, setCharacter] = useState<CharacterProfileResponse | null>(
    null
  );
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState<boolean>(false);
  const [submitError, setSubmitError] = useState<string | null>(null);

  const fetchCharacter = useCallback(
    async (id: number): Promise<CharacterProfileResponse | null> => {
      setIsLoading(true);
      setError(null);
      try {
        const response = await fetch(`${API_BASE_URL}/${id}`);
        if (!response.ok) {
          const message = await getApiErrorMessage(response);
          throw new Error(message);
        }
        const data: CharacterProfileResponse = await response.json();
        setCharacter(data);
        return data;
      } catch (err) {
        const message = getGenericErrorMessage(
          err,
          "キャラクターデータの読み込み"
        );
        console.error("Error fetching character data:", err);
        setError(message);
        setCharacter(null);
        return null;
      } finally {
        setIsLoading(false);
      }
    },
    []
  ); // 依存配列を適切に

  const createCharacter = useCallback(
    async (
      data: CreateCharacterProfileRequest
    ): Promise<CharacterProfileResponse | null> => {
      setIsSubmitting(true);
      setSubmitError(null);
      try {
        const response = await fetch(API_BASE_URL, {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify(data),
        });
        if (!response.ok) {
          const message = await getApiErrorMessage(response); // ★共通関数呼び出し
          throw new Error(message);
        }
        const createdData: CharacterProfileResponse = await response.json();
        return createdData;
      } catch (err) {
        const message = getGenericErrorMessage(err, "キャラクターの登録"); // ★共通関数呼び出し
        console.error("Error creating character:", err);
        setSubmitError(message);
        return null;
      } finally {
        setIsSubmitting(false);
      }
    },
    []
  );

  const updateCharacter = useCallback(
    async (
      id: number,
      data: UpdateCharacterProfileRequest
    ): Promise<boolean> => {
      setIsSubmitting(true);
      setSubmitError(null);
      try {
        const response = await fetch(`${API_BASE_URL}/${id}`, {
          method: "PUT",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify(data),
        });
        if (!response.ok) {
          const message = await getApiErrorMessage(response);
          throw new Error(message);
        }
        return true; // 成功
      } catch (err) {
        const message = getGenericErrorMessage(err, "キャラクターの更新");
        console.error("Error updating character:", err);
        setSubmitError(message);
        return false;
      } finally {
        setIsSubmitting(false);
      }
    },
    []
  );

  const deleteCharacter = useCallback(async (id: number): Promise<boolean> => {
    setIsSubmitting(true);
    setSubmitError(null);
    try {
      const response = await fetch(`${API_BASE_URL}/${id}`, {
        method: "DELETE",
      });
      if (!response.ok) {
        const message = await getApiErrorMessage(response);
        throw new Error(message);
      }
      return true; // 成功
    } catch (err) {
      const message = getGenericErrorMessage(err, "キャラクターの更新");
      console.error("Error deleting character:", err);
      setSubmitError(message);
      return false; // 失敗
    } finally {
      setIsSubmitting(false);
    }
  }, []);

  // フックが返す値（状態と関数）
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
