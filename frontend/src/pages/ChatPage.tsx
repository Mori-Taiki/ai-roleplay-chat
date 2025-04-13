import React, { useState, useEffect, useRef } from 'react';
import './ChatPage.css';

interface Message {
  id: number;
  sender: 'user' | 'ai';
  text: string;
  imageUrl?: string;
}

function ChatPage() {
  const [inputValue, setInputValue] = useState<string>('');
  const [messages, setMessages] = useState<Message[]>([]);
  const [messageIdCounter, setMessageIdCounter] = useState<number>(0);
  const [isLoading, setIsLoading] = useState<boolean>(false); // ローディング状態は共通で使う
  const chatWindowRef = useRef<HTMLDivElement>(null); // チャットウィンドウの参照

  /**
   * チャットウィンドウを一番下にスクロールする関数
   */
  const scrollToBottom = () => {
    if (chatWindowRef.current) {
      chatWindowRef.current.scrollTop = chatWindowRef.current.scrollHeight;
    }
  };

  // messages 配列が更新されるたびに一番下にスクロール
  useEffect(() => {
    scrollToBottom();
  }, [messages]);


  const handleInputChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    setInputValue(event.target.value);
  };

  /**
   * メッセージ送信をハンドルする非同期関数
   */
  const handleSendMessage = async () => {
    const trimmedInput = inputValue.trim();
    if (!trimmedInput || isLoading) {
      return;
    }

    // ユーザーメッセージを作成してすぐに追加 (UIの反応を良くするため)
    const newUserMessage: Message = {
      id: messageIdCounter,
      sender: 'user',
      text: trimmedInput,
    };
    setMessages(prevMessages => [...prevMessages, newUserMessage]);
    setMessageIdCounter(prevCounter => prevCounter + 1);
    setInputValue('');
    setIsLoading(true);

    const apiUrl = 'https://localhost:7000/api/chat'; // バックエンドのURLを確認

    try {
      const response = await fetch(apiUrl, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ Prompt: trimmedInput }),
      });

      if (!response.ok) {
        console.error('API Error Response:', response);
        const errorText = await response.text();
        throw new Error(`サーバーエラーが発生しました: ${response.status} ${errorText || response.statusText}`);
      }

      const data: { reply: string } = await response.json();

      const newAiMessage: Message = {
        id: messageIdCounter + 1,
        sender: 'ai',
        text: data.reply,
      };
      setMessages(prevMessages => [...prevMessages, newAiMessage]);
      setMessageIdCounter(prevCounter => prevCounter + 2);

    } catch (error) {
      console.error('API Call Failed:', error);
      const errorMessage: Message = {
        id: messageIdCounter + 1,
        sender: 'ai',
        text: `エラーが発生しました: ${error instanceof Error ? error.message : String(error)}`,
      };
      setMessages(prevMessages => [...prevMessages, errorMessage]);
      setMessageIdCounter(prevCounter => prevCounter + 2);
    } finally {
      setIsLoading(false);
    }
  };

  // ▼▼▼ 画像生成ボタンクリック時のハンドラ ▼▼▼
  const handleGenerateImage = async () => {
    const promptForImage = inputValue.trim();
    if (isLoading || !promptForImage) {
      console.log('画像生成スキップ（ローディング中または入力が空です）');
      return;
    }

    console.log(`画像生成リクエスト開始... プロンプト: "${promptForImage}"`);
    setIsLoading(true);
    setInputValue(''); 

    const imageUrl = 'https://localhost:7000/api/image/generate';

    try {
      const response = await fetch(imageUrl, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json', // ★ ヘッダーを追加
        },
        body: JSON.stringify({ Prompt: promptForImage }),
      });


      if (!response.ok) {
        console.error('Image API Error Response:', response);
        const errorText = await response.text();
        throw new Error(`画像生成APIエラー: ${response.status} ${errorText || response.statusText}`);
      }

      // 応答データをJSONとしてパース
      const imageData: { mimeType: string, base64Data: string } = await response.json();

      // Data URL を組み立てる
      const dataUrl = `data:${imageData.mimeType};base64,${imageData.base64Data}`;

      // 画像を含む新しいAIメッセージを作成
      const newImageMessage: Message = {
        id: messageIdCounter, // 画像単体なのでIDは1つ進める
        sender: 'ai',
        text: `「${promptForImage}」の画像を生成しました:`, 
        imageUrl: dataUrl, // 生成した画像のData URLを設定
      };

      // メッセージリストに画像メッセージを追加
      setMessages(prevMessages => [...prevMessages, newImageMessage]);
      setMessageIdCounter(prevCounter => prevCounter + 1); // IDを1つ進める

      console.log('画像生成成功！');

    } catch (error) {
      console.error('Image Generation API Call Failed:', error);
      // エラーメッセージをチャットに追加
      const errorMessage: Message = {
        id: messageIdCounter,
        sender: 'ai',
        text: `画像生成中にエラーが発生しました: ${error instanceof Error ? error.message : String(error)}`,
      };
      setMessages(prevMessages => [...prevMessages, errorMessage]);
      setMessageIdCounter(prevCounter => prevCounter + 1);
    } finally {
      setIsLoading(false); // ローディング終了
    }
  };
  // ▲▲▲ 画像生成ボタンクリック時のハンドラここまで ▲▲▲


  const handleKeyDown = (event: React.KeyboardEvent<HTMLInputElement>) => {
    // Enterキーでメッセージ送信 (画像生成はボタンクリックのみとする)
    if (event.key === 'Enter' && !isLoading) {
      handleSendMessage();
    }
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
            {msg.text && (
              <p className="message-text">{msg.text}</p>
            )}
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
        <button
          onClick={handleGenerateImage}
          disabled={isLoading}
          style={{ marginLeft: '5px' }}
        >
          {isLoading ? '生成中...' : '画像生成'}
        </button>
      </div>
    </div>
  );
}

export default ChatPage;

