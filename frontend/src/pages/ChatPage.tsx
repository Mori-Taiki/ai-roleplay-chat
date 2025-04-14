import React, { useState, useEffect, useCallback, useReducer } from 'react';
import { useParams } from 'react-router-dom';
import { useChatApi } from '../hooks/useChatApi';
import { v4 as uuidv4 } from 'uuid';
import styles from './ChatPage.module.css';

import MessageList from '../components/MessageList'; // インポート
import ChatInput from '../components/ChatInput'; // インポート

interface Message {
  id: string;
  sender: 'user' | 'ai';
  text: string;
  imageUrl?: string;
}

interface ChatState {
  messages: Message[];
}

type ChatAction =
  | { type: 'ADD_USER_MESSAGE'; payload: { text: string } }
  | { type: 'ADD_AI_MESSAGE'; payload: { text: string } }
  | { type: 'ADD_AI_IMAGE'; payload: { text: string; imageUrl: string } }
  | { type: 'ADD_ERROR_MESSAGE'; payload: { text: string } };

const initialState: ChatState = {
  messages: [],
};

// Reducer 関数: 現在の状態とアクションを受け取り、新しい状態を返す
function chatReducer(state: ChatState, action: ChatAction): ChatState {
  switch (action.type) {
    case 'ADD_USER_MESSAGE':
      return {
        ...state,
        messages: [...state.messages, { id: uuidv4(), sender: 'user', text: action.payload.text }],
      };
    case 'ADD_AI_MESSAGE':
      return {
        ...state,
        messages: [...state.messages, { id: uuidv4(), sender: 'ai', text: action.payload.text }],
      };
    case 'ADD_AI_IMAGE':
      return {
        ...state,
        messages: [
          ...state.messages,
          { id: uuidv4(), sender: 'ai', text: action.payload.text, imageUrl: action.payload.imageUrl },
        ],
      };
    case 'ADD_ERROR_MESSAGE':
      // エラーメッセージも AI メッセージとして表示する例
      return {
        ...state,
        messages: [...state.messages, { id: uuidv4(), sender: 'ai', text: `エラー: ${action.payload.text}` }],
      };
    default:
      // 未知のアクションタイプの場合は、状態を変更せずに返す
      // もしくはエラーをスローするなど、設計に応じて対応
      return state;
  }
}

function ChatPage() {
  const { id } = useParams<{ id: string }>();
  const characterId = parseInt(id ?? '0', 10);

  const [inputValue, setInputValue] = useState<string>('');
  const [state, dispatch] = useReducer(chatReducer, initialState);
  const { messages } = state;

  const { isSendingMessage, isGeneratingImage, sendMessage, generateImage, error: apiError } = useChatApi();
  const isLoading = isSendingMessage || isGeneratingImage;

  useEffect(() => {
    if (apiError) {
      dispatch({ type: 'ADD_ERROR_MESSAGE', payload: { text: apiError } });
      // TODO: エラーをクリアする処理 (例: ユーザー入力時、時間経過後など)
    }
  }, [apiError]);

  const handleSendMessageCallback = useCallback(async () => {
    const trimmedInput = inputValue.trim();
    if (!trimmedInput || isLoading || !characterId) return;

    // ユーザーメッセージ追加アクションを dispatch
    dispatch({ type: 'ADD_USER_MESSAGE', payload: { text: trimmedInput } });
    setInputValue('');

    const response = await sendMessage(characterId, trimmedInput /*, history */);

    if (response) {
      // AI メッセージ追加アクションを dispatch
      dispatch({ type: 'ADD_AI_MESSAGE', payload: { text: response.reply } });
    }
    // エラー処理は useEffect で apiError を監視して行われる
  }, [inputValue, isLoading, characterId, sendMessage /*, history */]);

  const handleGenerateImageCallback = useCallback(async () => {
    const promptForImage = inputValue.trim();
    if (isLoading || !promptForImage || !characterId) return;
    setInputValue('');

    const response = await generateImage(characterId, promptForImage);

    if (response) {
      const dataUrl = `data:${response.mimeType};base64,${response.base64Data}`;
      // 画像メッセージ追加アクションを dispatch
      dispatch({
        type: 'ADD_AI_IMAGE',
        payload: { text: `「${promptForImage}」の画像を生成しました:`, imageUrl: dataUrl },
      });
    }
  }, [inputValue, isLoading, characterId, generateImage]);

  const handleKeyDown = (event: React.KeyboardEvent<HTMLInputElement>) => {
    // Enterキーでメッセージ送信 (画像生成はボタンクリックのみとする)
    if (event.key === 'Enter' && !isLoading) {
      handleSendMessageCallback();
    }
  };

  const handleInputChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    setInputValue(event.target.value);
  };

  return (
    <div className={styles.pageContainer}>
      {' '}
      {/* ChatPage 用のルートコンテナ */}
      <h1>AIロールプレイチャット - キャラクターID: {characterId}</h1>
      {/* MessageList コンポーネントを使用 */}
      <MessageList messages={messages} isLoading={isLoading} />
      {/* ChatInput コンポーネントを使用 */}
      <ChatInput
        value={inputValue}
        onChange={handleInputChange}
        onSendMessage={handleSendMessageCallback}
        onGenerateImage={handleGenerateImageCallback}
        onKeyDown={handleKeyDown}
        isLoading={isLoading}
        // isSendDisabled={isSendingMessage} // 個別制御する場合
        // isImageGenDisabled={isGeneratingImage} // 個別制御する場合
      />
    </div>
  );
}

export default ChatPage;
