import React, { useState, useEffect, useCallback, useReducer } from 'react';
import { Link, useParams } from 'react-router-dom';
import { useChatApi } from '../hooks/useChatApi';
import { useCharacterProfile } from '../hooks/useCharacterProfile';
import { v4 as uuidv4 } from 'uuid';
import styles from './ChatPage.module.css';

import MessageList from '../components/MessageList';
import ChatInput from '../components/ChatInput';
import { Message } from '../models/Message';

type ChatAction =
  | { type: 'SET_HISTORY'; payload: Message[] }
  | { type: 'ADD_USER_MESSAGE'; payload: { text: string } }
  | { type: 'ADD_AI_RESPONSE'; payload: { text: string; id: string, requiresImageGeneration: boolean, } }
  | { type: 'UPDATE_IMAGE_URL'; payload: { messageId: string; imageUrl: string } }
  | { type: 'ADD_ERROR_MESSAGE'; payload: { text: string } };
interface DisplayMessage extends Message {
  isImageLoading?: boolean;
}
interface ChatState {
  messages: DisplayMessage[];
}
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
    case 'UPDATE_IMAGE_URL':
      return {
        ...state,
        messages: state.messages.map((msg) =>
          msg.id === action.payload.messageId
            ? { ...msg, imageUrl: action.payload.imageUrl, isImageLoading: false }
            : msg
        ),
      };
    case 'ADD_AI_RESPONSE':
      return {
        ...state,
        messages: [
          ...state.messages,
          {
            id: action.payload.id,
            sender: 'ai',
            text: action.payload.text,
             isImageLoading: action.payload.requiresImageGeneration,
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
    sendMessage,
    generateAndUploadImage,
    error: apiError,
    isLoadingHistory,
    fetchHistory,
    isLoadingLatestSession,
    fetchLatestSessionId,
  } = useChatApi();
  const isLoading = isSendingMessage || isLoadingCharacter;

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

  const handleSendMessage = useCallback(
    async (prompt: string) => {
      if (!prompt || isLoading || !characterId) return;

      dispatch({ type: 'ADD_USER_MESSAGE', payload: { text: prompt } });
      if (prompt === inputValue) {
        setInputValue('');
      }

      const response = await sendMessage(characterId, prompt, currentSessionId);

      if (response) {
        // 1. まずテキスト応答をReducerに渡して表示
        //    バックエンドから受け取ったメッセージIDを文字列に変換して使う
        const aiMessageId = response.aiMessageId.toString();
        dispatch({
          type: 'ADD_AI_RESPONSE',
          payload: { text: response.reply, id: aiMessageId, requiresImageGeneration: response.requiresImageGeneration, },
        });
        setCurrentSessionId(response.sessionId);

        // 2. 画像生成が必要な場合、非同期で画像生成APIを呼び出す
        if (response.requiresImageGeneration) {
          const imageResponse = await generateAndUploadImage(response.aiMessageId);
          if (imageResponse) {
            // 3. 成功したら、画像URLをReducerに渡して更新
            dispatch({
              type: 'UPDATE_IMAGE_URL',
              payload: { messageId: aiMessageId, imageUrl: imageResponse.imageUrl },
            });
          } else {
            // 画像生成失敗時のハンドリング (例: エラーメッセージをコンソールに出す、など)
            console.error('Image generation failed for message ID:', aiMessageId);
            // UI上もローディングを解除
            dispatch({
              type: 'UPDATE_IMAGE_URL',
              payload: { messageId: aiMessageId, imageUrl: '' }, // 空文字などでローディング解除
            });
          }
        }
      }
    },
    [inputValue, isLoading, characterId, currentSessionId, sendMessage, generateAndUploadImage, dispatch]
  );

  const handleRetry = useCallback(
    async (prompt: string) => {
      if (isLoading || !characterId) return;

      const response = await sendMessage(characterId, prompt, currentSessionId);

      if (response) {
        const aiMessageId = response.aiMessageId.toString();
        dispatch({
          type: 'ADD_AI_RESPONSE',
          payload: { text: response.reply, id: aiMessageId, requiresImageGeneration: response.requiresImageGeneration, },
        });
        setCurrentSessionId(response.sessionId);
        if (response.requiresImageGeneration) {
          const imageResponse = await generateAndUploadImage(response.aiMessageId);
          if (imageResponse) {
            dispatch({
              type: 'UPDATE_IMAGE_URL',
              payload: { messageId: aiMessageId, imageUrl: imageResponse.imageUrl },
            });
          } else {
            console.error('Image generation failed for message ID:', aiMessageId);
            dispatch({
              type: 'UPDATE_IMAGE_URL',
              payload: { messageId: aiMessageId, imageUrl: '' },
            });
          }
        }
      }
    },
    [isLoading, characterId, sendMessage, currentSessionId, generateAndUploadImage]
  );

  const handleKeyDown = (event: React.KeyboardEvent<HTMLInputElement>) => {
    if (event.key === 'Enter' && !isLoading) {
      handleSendMessage(inputValue);
    }
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
        isLoading={isSendingMessage}
        onRetry={handleRetry}
      />
      <ChatInput
        value={inputValue}
        onChange={(e) => setInputValue(e.target.value)}
        onSendMessage={() => handleSendMessage(inputValue)}
        onKeyDown={handleKeyDown}
        isLoading={isLoading}
      />
    </div>
  );
}

export default ChatPage;
