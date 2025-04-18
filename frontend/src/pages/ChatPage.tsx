import React, { useState, useEffect, useCallback, useReducer } from 'react';
import { Link, useParams } from 'react-router-dom';
import { useChatApi } from '../hooks/useChatApi';
import { useCharacterProfile } from '../hooks/useCharacterProfile';
import { v4 as uuidv4 } from 'uuid';
import styles from './ChatPage.module.css';

import MessageList from '../components/MessageList'; // インポート
import ChatInput from '../components/ChatInput'; // インポート
import { Message } from '../models/Message';

interface ChatState {
  messages: Message[];
}

type ChatAction =
  | { type: 'SET_HISTORY'; payload: Message[] }
  | { type: 'ADD_USER_MESSAGE'; payload: { text: string } }
  | { type: 'ADD_AI_RESPONSE'; payload: { text: string; imageUrl?: string } }
  // | { type: 'ADD_AI_MESSAGE'; payload: { text: string } }
  // | { type: 'ADD_AI_IMAGE'; payload: { text: string; imageUrl: string } }
  | { type: 'ADD_ERROR_MESSAGE'; payload: { text: string } };

const initialState: ChatState = {
  messages: [],
};

// Reducer 関数: 現在の状態とアクションを受け取り、新しい状態を返す
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
            text: action.payload.text, // ペイロードからテキストを取得
            imageUrl: action.payload.imageUrl, // ペイロードから画像URLを取得 (undefined かもしれない)
          },
        ],
      };
    // case 'ADD_AI_MESSAGE':
    //   return {
    //     ...state,
    //     messages: [...state.messages, { id: uuidv4(), sender: 'ai', text: action.payload.text }],
    //   };
    // case 'ADD_AI_IMAGE':
    //   return {
    //     ...state,
    //     messages: [
    //       ...state.messages,
    //       { id: uuidv4(), sender: 'ai', text: action.payload.text, imageUrl: action.payload.imageUrl },
    //     ],
    //   };
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
    // generateImage,
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
      // TODO: エラーをクリアする処理 (例: ユーザー入力時、時間経過後など)
    }
  }, [apiError]);

  useEffect(() => {
    if (characterId > 0) {
      // 有効な characterId の場合のみ実行
      console.log(`Workspaceing character profile for ID: ${characterId}`);
      fetchCharacter(characterId);
    }
    // characterId が変わったら再取得
  }, [characterId, fetchCharacter]);

  useEffect(() => {
    if (!characterId) return; // キャラクターIDがなければ何もしない

    const loadLatestSession = async () => {
      const latestSessionId = await fetchLatestSessionId(characterId);
      if (latestSessionId) {
        setCurrentSessionId(latestSessionId);
        console.log('Latest session ID loaded:', latestSessionId);
      } else {
        // アクティブなセッションがない場合は null のまま（新規セッション扱い）
        console.log('No active session found, starting a new one.');
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
        // セッションIDがない場合（新規）は履歴を空にする
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

    // sendMessage は ChatResponse ({ Reply, SessionId, ImageUrl? }) | null を返す
    const response = await sendMessage(characterId, trimmedInput, currentSessionId /*, history */);

    if (response) {
      // ★ ADD_AI_RESPONSE アクションを dispatch するように変更
      dispatch({
        type: 'ADD_AI_RESPONSE',
        payload: {
          text: response.reply, // AIのテキスト応答
          imageUrl: response.imageUrl, // 生成された画像のURL (nullかもしれない)
        },
      });
      // セッションIDの更新はそのまま
      setCurrentSessionId(response.sessionId);
    }
    // エラー処理は useEffect で apiError を監視して行われる
  }, [inputValue, isLoading, characterId, currentSessionId, sendMessage, dispatch]);

  // const handleGenerateImageCallback = useCallback(async () => {
  //   const promptForImage = inputValue.trim();
  //   if (isLoading || !promptForImage || !characterId) return;
  //   setInputValue('');

  //   const response = await generateImage(characterId, promptForImage);

  //   if (response) {
  //     const dataUrl = `data:${response.mimeType};base64,${response.base64Data}`;
  //     // 画像メッセージ追加アクションを dispatch
  //     dispatch({
  //       type: 'ADD_AI_IMAGE',
  //       payload: { text: `「${promptForImage}」の画像を生成しました:`, imageUrl: dataUrl },
  //     });
  //   }
  // }, [inputValue, isLoading, characterId, generateImage]);

  const handleKeyDown = (event: React.KeyboardEvent<HTMLInputElement>) => {
    // Enterキーでメッセージ送信 (画像生成はボタンクリックのみとする)
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
      {/* MessageList コンポーネントを使用 */}
      <MessageList
        characterName={character?.name ?? '不明なキャラクター'}
        messages={messages}
        isLoading={isSendingMessage || isGeneratingImage}
      />
      {/* ChatInput コンポーネントを使用 */}
      <ChatInput
        value={inputValue}
        onChange={handleInputChange}
        onSendMessage={handleSendMessageCallback}
        // onGenerateImage={handleGenerateImageCallback}
        onKeyDown={handleKeyDown}
        isLoading={isLoading}
        // isSendDisabled={isSendingMessage} // 個別制御する場合
        // isImageGenDisabled={isGeneratingImage} // 個別制御する場合
      />
    </div>
  );
}

export default ChatPage;
