import React, { useState, useEffect, useRef } from 'react'; // useEffect と useRef をインポート
import './App.css';

interface Message {
  id: number;
  sender: 'user' | 'ai';
  text: string;
}

function App() {
  const [inputValue, setInputValue] = useState<string>('');
  const [messages, setMessages] = useState<Message[]>([]);
  const [messageIdCounter, setMessageIdCounter] = useState<number>(0);
  const [isLoading, setIsLoading] = useState<boolean>(false); // ローディング状態を追加
  const chatWindowRef = useRef<HTMLDivElement>(null); // チャットウィンドウの参照を追加

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
    if (!trimmedInput || isLoading) { // ローディング中は送信しない
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
    setInputValue(''); // 入力欄をクリア
    setIsLoading(true); // ローディング開始

    // --- バックエンドAPI呼び出し ---
    const apiUrl = 'http://localhost:5129/api/chat';

    try {
      const response = await fetch(apiUrl, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        // C#側のChatRequestレコードに合わせて 'Prompt' (大文字P) で送信
        body: JSON.stringify({ Prompt: trimmedInput }),
      });

      if (!response.ok) {
        // エラーレスポンスの場合
        console.error('API Error Response:', response);
        const errorText = await response.text(); // エラー内容をテキストで取得試行
        throw new Error(`サーバーエラーが発生しました: ${response.status} ${errorText || response.statusText}`);
      }

      // 正常なレスポンスの場合 (JSONをパース)
      const data: { reply: string } = await response.json(); // 応答の型を仮定

      // AIの応答メッセージオブジェクトを作成
      const newAiMessage: Message = {
        id: messageIdCounter + 1, // ユーザーメッセージの次のID
        sender: 'ai',
        text: data.reply, // APIからの応答テキストを使用
      };

      // メッセージリストにAIの応答を追加
      setMessages(prevMessages => [...prevMessages, newAiMessage]);
      setMessageIdCounter(prevCounter => prevCounter + 2); // AIメッセージの分もIDを進める

    } catch (error) {
      console.error('API Call Failed:', error);
      // エラーメッセージをチャットに追加する (任意)
      const errorMessage: Message = {
        id: messageIdCounter + 1,
        sender: 'ai',
        text: `エラーが発生しました: ${error instanceof Error ? error.message : String(error)}`,
      };
      setMessages(prevMessages => [...prevMessages, errorMessage]);
      setMessageIdCounter(prevCounter => prevCounter + 2);
    } finally {
      setIsLoading(false); // ローディング終了
    }
     // --- API呼び出しここまで ---
  };

  const handleKeyDown = (event: React.KeyboardEvent<HTMLInputElement>) => {
    if (event.key === 'Enter' && !isLoading) { // ローディング中はEnter無効
      handleSendMessage();
    }
  };


  return (
    <div className="app-container">
      <h1>AIロールプレイチャット (仮)</h1>
      {/* chatWindowにrefを設定 */}
      <div className="chat-window" ref={chatWindowRef}>
        {messages.map((msg) => (
          <div key={msg.id} className={`message ${msg.sender}`}>
            <span className="sender-label">{msg.sender === 'user' ? 'あなた' : 'AI'}</span>
            <p className="message-text">{msg.text}</p>
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
          disabled={isLoading} // ローディング中は入力不可
        />
        <button onClick={handleSendMessage} disabled={isLoading}> {/* ローディング中はボタン無効 */}
          {isLoading ? '送信中...' : '送信'}
        </button>
      </div>
    </div>
  );
}

export default App;

