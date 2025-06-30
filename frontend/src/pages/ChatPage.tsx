import React, { useState, useEffect, useCallback, useReducer } from 'react';
import { Link, useParams } from 'react-router-dom';
import { useChatApi } from '../hooks/useChatApi';
import { useCharacterProfile } from '../hooks/useCharacterProfile';
import { v4 as uuidv4 } from 'uuid';
import styles from './ChatPage.module.css';

import MessageList from '../components/MessageList';
import ChatInput from '../components/ChatInput';
import { Message } from '../models/Message';

interface ChatState {
  messages: Message[];
}

type ChatAction =
  | { type: 'SET_HISTORY'; payload: Message[] }
  | { type: 'ADD_USER_MESSAGE'; payload: { text: string } }
  | { type: 'ADD_AI_RESPONSE'; payload: { text: string; imageUrl?: string } }
  | { type: 'ADD_ERROR_MESSAGE'; payload: { text: string } };

const initialState: ChatState = {
  messages: [],
};

function chatReducer(state: ChatState, action: ChatAction): ChatState {
  switch (action.type) {
    case 'SET_HISTORY':
      return { ...state, messages: action.payload };
    case 'ADD_USER_MESSAGE':
      return {
        ...state,
        messages: [...state.messages, { id: uuidv4(), sender: 'user', text: action.payload.text }],
      };
    case 'ADD_AI_RESPONSE':
      return {
        ...state,
        messages: [
          ...state.messages,
          {
            id: uuidv4(),
            sender: 'ai',
            text: action.payload.text,
            imageUrl: action.payload.imageUrl,
          },
        ],
      };
    case 'ADD_ERROR_MESSAGE':
      return {
        ...state,
        messages: [...state.messages, { id: uuidv4(), sender: 'ai', text: `エラー: ${action.payload.text}` }],
      };
    default:
      return state;
  }
}

function ChatPage() {
  const { id } = useParams<{ id: string }>();
  const characterId = parseInt(id ?? '0', 10);

  const [inputValue, setInputValue] = useState<string>('');
  const [state, dispatch] = useReducer(chatReducer, initialState);
  const [currentSessionId, setCurrentSessionId] = useState<string | null>(null);
  const { messages } = state;

  const {
    character,
    isLoading: isLoadingCharacter,
    error: characterFetchError,
    fetchCharacter,
  } = useCharacterProfile();

  const {
    isSendingMessage,
    isGeneratingImage,
    sendMessage,
    error: apiError,
    isLoadingHistory,
    fetchHistory,
    isLoadingLatestSession,
    fetchLatestSessionId,
  } = useChatApi();
  const isLoading =
    isSendingMessage || isGeneratingImage || isLoadingHistory || isLoadingLatestSession || isLoadingCharacter;

  useEffect(() => {
    if (apiError) {
      dispatch({ type: 'ADD_ERROR_MESSAGE', payload: { text: apiError } });
    }
  }, [apiError]);

  useEffect(() => {
    if (characterId > 0) {
      fetchCharacter(characterId);
    }
  }, [characterId, fetchCharacter]);

  useEffect(() => {
    if (!characterId) return;
    const loadLatestSession = async () => {
      const latestSessionId = await fetchLatestSessionId(characterId);
      if (latestSessionId) {
        setCurrentSessionId(latestSessionId);
      }
    };
    loadLatestSession();
  }, [characterId, fetchLatestSessionId]);

  useEffect(() => {
    const loadHistory = async () => {
      if (currentSessionId) {
        const history = await fetchHistory(currentSessionId);
        if (history) {
          dispatch({ type: 'SET_HISTORY', payload: history });
        }
      } else {
        dispatch({ type: 'SET_HISTORY', payload: [] });
      }
    };
    loadHistory();
  }, [currentSessionId, fetchHistory]);

  const handleSendMessageCallback = useCallback(async () => {
    const trimmedInput = inputValue.trim();
    if (!trimmedInput || isLoading || !characterId) return;
    dispatch({ type: 'ADD_USER_MESSAGE', payload: { text: trimmedInput } });
    setInputValue('');
    const response = await sendMessage(characterId, trimmedInput, currentSessionId);
    if (response) {
      dispatch({
        type: 'ADD_AI_RESPONSE',
        payload: {
          text: response.reply,
          imageUrl: response.imageUrl,
        },
      });
      setCurrentSessionId(response.sessionId);
    }
  }, [inputValue, isLoading, characterId, currentSessionId, sendMessage, dispatch]);

  const handleRetry = useCallback(async (prompt: string) => {
    if (isLoading || !characterId) return;

    const response = await sendMessage(characterId, prompt, currentSessionId);

    if (response) {
      dispatch({
        type: 'ADD_AI_RESPONSE',
        payload: {
          text: response.reply,
          imageUrl: response.imageUrl,
        },
      });
      setCurrentSessionId(response.sessionId);
    }
  }, [isLoading, characterId, sendMessage, currentSessionId, dispatch]);

  const handleKeyDown = (event: React.KeyboardEvent<HTMLInputElement>) => {
    if (event.key === 'Enter' && !isLoading) {
      handleSendMessageCallback();
    }
  };

  const handleInputChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    setInputValue(event.target.value);
  };

  if (characterFetchError) {
    return (
      <div className={styles.pageContainer}>
        <div style={{ color: 'red', padding: '1rem' }}>
          キャラクター情報の読み込みに失敗しました: {characterFetchError}
          <br />
          <Link to="/characters">キャラクター一覧に戻る</Link>
        </div>
      </div>
    );
  }

  return (
    <div className={styles.pageContainer}>
      <h1>
        {isLoadingCharacter ? 'キャラクター情報読み込み中...' : `${character?.name ?? '不明なキャラクター'} `}
        <img
          src={
            character?.avatarImageUrl ||
            'https://airoleplaychatblobstr.blob.core.windows.net/profile-images/placeholder.png'
          }
          alt={character?.name || 'プレースホルダー'}
          style={{
            height: '30px',
            width: '30px',
            borderRadius: '50%',
            marginLeft: '10px',
            verticalAlign: 'middle',
          }}
        />
      </h1>
      {(isLoadingLatestSession || isLoadingHistory) && <div>履歴を読み込み中...</div>}
      <MessageList
        characterName={character?.name ?? '不明なキャラクター'}
        messages={messages}
        isLoading={isSendingMessage || isGeneratingImage}
        onRetry={handleRetry} 
      />
      <ChatInput
        value={inputValue}
        onChange={handleInputChange}
        onSendMessage={handleSendMessageCallback}
        onKeyDown={handleKeyDown}
        isLoading={isLoading}
      />
    </div>
  );
}

export default ChatPage;