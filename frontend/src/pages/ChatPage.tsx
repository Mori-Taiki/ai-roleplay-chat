import React, { useState, useEffect, useRef, useCallback } from 'react';
import { useParams } from 'react-router-dom'; // useParams をインポート
import { useChatApi } from '../hooks/useChatApi'; // 作成したフックをインポート
import './ChatPage.css';

interface Message {
  id: number;
  sender: 'user' | 'ai';
  text: string;
  imageUrl?: string;
}

function ChatPage() {
  const { id } = useParams<{ id: string }>();
  const characterId = parseInt(id ?? '0', 10);

  const [inputValue, setInputValue] = useState<string>('');
  const [messages, setMessages] = useState<Message[]>([]);
  const [messageIdCounter, setMessageIdCounter] = useState<number>(0); // UUID などに変更推奨
  const chatWindowRef = useRef<HTMLDivElement>(null);

  // カスタムフックを使用
  const { isSendingMessage, isGeneratingImage, sendMessage, generateImage, error: apiError } = useChatApi();

  // isLoading は isSendingMessage と isGeneratingImage を組み合わせる
  const isLoading = isSendingMessage || isGeneratingImage;

  // API エラーを監視してメッセージに追加する (エラー表示方法は要検討)
  useEffect(() => {
    if (apiError) {
      const errorMsg: Message = { id: Date.now(), sender: 'ai', text: `エラー: ${apiError}` };
      setMessages((prev) => [...prev, errorMsg]);
      // TODO: エラーをクリアする手段も必要
    }
  }, [apiError]);

  const handleSendMessage = useCallback(async () => {
    const trimmedInput = inputValue.trim();
    if (!trimmedInput || isLoading || !characterId) return;

    const newUserMessage: Message = {
      id: messageIdCounter,
      sender: 'user',
      text: trimmedInput,
    };
    setMessages((prev) => [...prev, newUserMessage]);
    setMessageIdCounter((prev) => prev + 1);
    setInputValue('');

    // カスタムフックの関数を呼び出し
    const response = await sendMessage(characterId, trimmedInput /*, messageHistory */); // 履歴も渡す

    if (response) {
      const newAiMessage: Message = {
        id: messageIdCounter,
        sender: 'ai',
        text: response.reply,
      };
      setMessages((prev) => [...prev, newAiMessage]);
      setMessageIdCounter((prev) => prev + 2);
    }
    // エラー処理はフック側で行われ、apiError state に反映される
  }, [inputValue, isLoading, characterId, sendMessage /*, messageHistory */]); // 依存配列

  const handleGenerateImage = useCallback(async () => {
    const promptForImage = inputValue.trim();
    if (isLoading || !promptForImage || !characterId) return;
    setInputValue('');

    const response = await generateImage(characterId, promptForImage);

    if (response) {
      const dataUrl = `data:${response.mimeType};base64,${response.base64Data}`;
      const newImageMessage: Message = {
        id: messageIdCounter,
        sender: 'ai',
        text: `「${promptForImage}」の画像を生成しました:`,
        imageUrl: dataUrl,
      };
      setMessages((prev) => [...prev, newImageMessage]);
      setMessageIdCounter((prev) => prev + 1);
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
