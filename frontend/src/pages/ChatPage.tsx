import React, { useState, useEffect, useRef, useCallback, useReducer } from 'react';
import { useParams } from 'react-router-dom';
import { useChatApi } from '../hooks/useChatApi';
import { v4 as uuidv4 } from 'uuid';
import './ChatPage.css';

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

  const chatWindowRef = useRef<HTMLDivElement>(null);
  const { isSendingMessage, isGeneratingImage, sendMessage, generateImage, error: apiError } = useChatApi();
  const isLoading = isSendingMessage || isGeneratingImage;

  useEffect(() => {
    if (apiError) {
      dispatch({ type: 'ADD_ERROR_MESSAGE', payload: { text: apiError } });
      // TODO: エラーをクリアする処理 (例: ユーザー入力時、時間経過後など)
    }
  }, [apiError]);

  const handleSendMessage = useCallback(async () => {
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

  const handleGenerateImage = useCallback(async () => {
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
      handleSendMessage();
    }
  };

  const handleInputChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    setInputValue(event.target.value);
  };

  return (
    <div className="app-container">
      <h1>AIロールプレイチャット (仮)</h1>
      <div className="chat-window" ref={chatWindowRef}>
        {messages.map((msg) => (
          <div key={msg.id} className={`message ${msg.sender}`}>
            <span className="sender-label">{msg.sender === 'user' ? 'あなた' : 'AI'}</span>
            {/* 画像URLがあれば画像を表示 */}
            {msg.imageUrl && (
              <img
                src={msg.imageUrl}
                alt="生成された画像"
                style={{ maxWidth: '80%', maxHeight: '300px', marginTop: '5px', display: 'block' }} // スタイルはお好みで
              />
            )}
            {/* テキストがあればテキストを表示 */}
            {msg.text && <p className="message-text">{msg.text}</p>}
          </div>
        ))}
        {/* ローディング表示 */}
        {isLoading && (
          <div className="message ai loading">
            <span className="sender-label">AI</span>
            <p className="message-text">考え中...</p>
          </div>
        )}
      </div>
      <div className="input-area">
        <input
          type="text"
          value={inputValue}
          onChange={handleInputChange}
          onKeyDown={handleKeyDown}
          placeholder="メッセージを入力..."
          disabled={isLoading}
        />
        <button onClick={handleSendMessage} disabled={isLoading}>
          {isLoading ? '送信中...' : '送信'}
        </button>
        <button onClick={handleGenerateImage} disabled={isLoading} style={{ marginLeft: '5px' }}>
          {isLoading ? '生成中...' : '画像生成'}
        </button>
      </div>
    </div>
  );
}

export default ChatPage;
