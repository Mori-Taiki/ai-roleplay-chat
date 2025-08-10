import { useState, useEffect, useCallback, useReducer } from 'react';
import { Link, useParams } from 'react-router-dom';
import { useChatApi } from '../hooks/useChatApi';
import { useCharacterProfile } from '../hooks/useCharacterProfile';
import { useNotification } from '../hooks/useNotification';
import { v4 as uuidv4 } from 'uuid';
import styles from './ChatPage.module.css';

import MessageList from '../components/MessageList';
import ChatInput from '../components/ChatInput';
import { Message } from '../models/Message';

type ChatAction =
  | { type: 'SET_HISTORY'; payload: Message[] }
  | { type: 'ADD_USER_MESSAGE'; payload: { text: string; id: string } }
  | { type: 'ADD_AI_RESPONSE'; payload: { text: string; id: string, requiresImageGeneration: boolean, } }
  | { type: 'START_IMAGE_GENERATION'; payload: { messageId: string } } // ★ 追加
  | { type: 'UPDATE_IMAGE_URL'; payload: { messageId: string; imageUrl: string } }
  | { type: 'SET_MESSAGE_ERROR'; payload: { messageId: string; isError: boolean } }
  | { type: 'UPDATE_USER_MESSAGE'; payload: { messageId: string; newText: string } }
  | { type: 'UPDATE_AI_MESSAGE'; payload: { messageId: string; newText: string; requiresImageGeneration: boolean } };
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
        messages: [...state.messages, { id: action.payload.id, sender: 'user', text: action.payload.text }],
      };
    // ★ START_IMAGE_GENERATION アクションの処理を追加
    case 'START_IMAGE_GENERATION':
      return {
        ...state,
        messages: state.messages.map((msg) =>
          msg.id === action.payload.messageId
            ? { ...msg, isImageLoading: true }
            : msg
        ),
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
    case 'SET_MESSAGE_ERROR':
      return {
        ...state,
        messages: state.messages.map((msg) =>
          msg.id === action.payload.messageId
            ? { ...msg, isError: action.payload.isError }
            : msg
        ),
      };
    case 'UPDATE_USER_MESSAGE':
      return {
        ...state,
        messages: state.messages.map((msg) =>
          msg.id === action.payload.messageId
            ? { ...msg, text: action.payload.newText, isError: false }
            : msg
        ),
      };
    case 'UPDATE_AI_MESSAGE':
      return {
        ...state,
        messages: state.messages.map((msg) =>
          msg.id === action.payload.messageId
            ? {
                ...msg,
                text: action.payload.newText,
                isImageLoading: action.payload.requiresImageGeneration,
                ...(action.payload.requiresImageGeneration ? { imageUrl: undefined } : {}),
              }
            : msg
        ),
      };
    default:
      return state;
  }
}

function ChatPage() {
  const { id, characterId: charIdParam, sessionId } = useParams<{ id?: string; characterId?: string; sessionId?: string }>();
  // Handle both route patterns: /chat/:id and /chat/:characterId/:sessionId
  const characterId = parseInt(charIdParam || id || '0', 10);

  const [inputValue, setInputValue] = useState<string>('');
  const [state, dispatch] = useReducer(chatReducer, initialState);
  const [currentSessionId, setCurrentSessionId] = useState<string | null>(sessionId || null);
  const { messages } = state;
  const { addNotification } = useNotification();

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
    editAndRegenerate,
    regenerateAi,
  } = useChatApi();
  const isLoading = isSendingMessage || isLoadingCharacter;

  useEffect(() => {
    if (apiError) {
      addNotification({
        message: apiError,
        type: 'error',
      });
    }
  }, [apiError, addNotification]);

  useEffect(() => {
    if (characterId > 0) {
      fetchCharacter(characterId);
    }
  }, [characterId, fetchCharacter]);

  useEffect(() => {
    if (!characterId) return;
    // Only fetch latest session if no specific session ID was provided in the URL
    if (!sessionId) {
      const loadLatestSession = async () => {
        const latestSessionId = await fetchLatestSessionId(characterId);
        if (latestSessionId) {
          setCurrentSessionId(latestSessionId);
        }
      };
      loadLatestSession();
    }
  }, [characterId, sessionId, fetchLatestSessionId]);

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

      const tempUserId = uuidv4();
      dispatch({ type: 'ADD_USER_MESSAGE', payload: { text: prompt, id: tempUserId } });
      if (prompt === inputValue) {
        setInputValue('');
      }

      const response = await sendMessage(characterId, prompt, currentSessionId);

      if (response) {
        // Success - clear any error state
        dispatch({ type: 'SET_MESSAGE_ERROR', payload: { messageId: tempUserId, isError: false } });
        
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
      } else {
        // Set error for the user message that just failed
        dispatch({ type: 'SET_MESSAGE_ERROR', payload: { messageId: tempUserId, isError: true } });
      }
    },
    [inputValue, isLoading, characterId, currentSessionId, sendMessage, generateAndUploadImage, dispatch]
  );
  
  // ★ 新しいハンドラを追加
  const handleGenerateImageForMessage = useCallback(
    async (messageId: string) => {
      if (isLoading || !messageId) return;
      
      const messageIdAsNumber = parseInt(messageId, 10);
      if(isNaN(messageIdAsNumber)) return;

      // 1. UIをローディング状態にする
      dispatch({ type: 'START_IMAGE_GENERATION', payload: { messageId } });

      // 2. 画像生成APIを呼び出す
      const imageResponse = await generateAndUploadImage(messageIdAsNumber);

      if (imageResponse) {
        // 3. 成功したら画像URLで更新
        dispatch({
          type: 'UPDATE_IMAGE_URL',
          payload: { messageId, imageUrl: imageResponse.imageUrl },
        });
      } else {
        // 4. 失敗したらローディングを解除
        console.error('Manual image generation failed for message ID:', messageId);
        dispatch({
          type: 'UPDATE_IMAGE_URL',
          payload: { messageId, imageUrl: '' }, // isImageLoading: false にする
        });
        // エラーをポップアップ通知で表示
        addNotification({
          message: '画像の生成に失敗しました。',
          type: 'error',
        });
      }
    },
    [isLoading, generateAndUploadImage, dispatch, addNotification]
  );

  const handleEditMessage = useCallback(
    async (messageId: string, newText: string) => {
      if (isLoading || !characterId) return;

      const messageIdAsNumber = parseInt(messageId, 10);
      if (isNaN(messageIdAsNumber)) return;

      const response = await editAndRegenerate(messageIdAsNumber, newText);

      if (response) {
        // Update the user message text
        dispatch({
          type: 'UPDATE_USER_MESSAGE',
          payload: { messageId, newText },
        });

        // Update/replace the AI response
        const aiMessageId = response.aiMessageId.toString();
        dispatch({
          type: 'UPDATE_AI_MESSAGE',
          payload: { messageId: aiMessageId, newText: response.reply, requiresImageGeneration: response.requiresImageGeneration },
        });

        setCurrentSessionId(response.sessionId);

        // Handle image generation if needed
        if (response.requiresImageGeneration) {
          const imageResponse = await generateAndUploadImage(response.aiMessageId);
          if (imageResponse) {
            dispatch({
              type: 'UPDATE_IMAGE_URL',
              payload: { messageId: aiMessageId, imageUrl: imageResponse.imageUrl },
            });
          } else {
            console.error('Image generation failed for edited message ID:', aiMessageId);
            dispatch({
              type: 'UPDATE_IMAGE_URL',
              payload: { messageId: aiMessageId, imageUrl: '' },
            });
          }
        }
      }
    },
    [isLoading, characterId, editAndRegenerate, generateAndUploadImage, dispatch]
  );

  const handleRegenerateAi = useCallback(
    async (messageId: string) => {
      if (isLoading || !characterId) return;

      const messageIdAsNumber = parseInt(messageId, 10);
      if (isNaN(messageIdAsNumber)) return;

      const response = await regenerateAi(messageIdAsNumber);

      if (response) {
        // Update the AI message
        dispatch({
          type: 'UPDATE_AI_MESSAGE',
          payload: { messageId, newText: response.reply, requiresImageGeneration: response.requiresImageGeneration },
        });

        setCurrentSessionId(response.sessionId);

        // Handle image generation if needed
        if (response.requiresImageGeneration) {
          const imageResponse = await generateAndUploadImage(response.aiMessageId);
          if (imageResponse) {
            dispatch({
              type: 'UPDATE_IMAGE_URL',
              payload: { messageId, imageUrl: imageResponse.imageUrl },
            });
          } else {
            console.error('Image generation failed for regenerated AI message ID:', messageId);
            dispatch({
              type: 'UPDATE_IMAGE_URL',
              payload: { messageId, imageUrl: '' },
            });
          }
        }
      }
    },
    [isLoading, characterId, regenerateAi, generateAndUploadImage, dispatch]
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
        onGenerateImage={handleGenerateImageForMessage}
        onEditMessage={handleEditMessage}
        onRegenerateAi={handleRegenerateAi}
      />
      <ChatInput
        value={inputValue}
        onChange={setInputValue}
        onSendMessage={() => handleSendMessage(inputValue)}
        isLoading={isLoading}
      />
    </div>
  );
}

export default ChatPage;