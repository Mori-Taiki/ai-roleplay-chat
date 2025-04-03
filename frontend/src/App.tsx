import React, { useState } from 'react';
import './App.css'; // CSSファイルをインポート (後述)

// メッセージの型定義
interface Message {
  id: number; // メッセージを識別するための一意なID
  sender: 'user' | 'ai'; // 送信者（ユーザーかAIか）
  text: string; // メッセージ本文
}

function App() {
  // 入力中のメッセージを保持するstate
  const [inputValue, setInputValue] = useState<string>('');
  // 会話のメッセージリストを保持するstate
  const [messages, setMessages] = useState<Message[]>([]);
  // メッセージID用カウンター (簡易的な実装)
  const [messageIdCounter, setMessageIdCounter] = useState<number>(0);

  /**
   * 入力欄の変更をハンドルする関数
   * @param event input要素のチェンジイベント
   */
  const handleInputChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    setInputValue(event.target.value);
  };

  /**
   * メッセージ送信をハンドルする関数
   */
  const handleSendMessage = () => {
    // 入力が空の場合は何もしない
    if (!inputValue.trim()) {
      return;
    }

    // 新しいユーザーメッセージオブジェクトを作成
    const newUserMessage: Message = {
      id: messageIdCounter,
      sender: 'user',
      text: inputValue.trim(),
    };

    // メッセージリストに新しいメッセージを追加
    // TODO: 本来はこの後、バックエンドAPIを呼び出す
    setMessages(prevMessages => [...prevMessages, newUserMessage]);

    // メッセージIDカウンターをインクリメント
    setMessageIdCounter(prevCounter => prevCounter + 1);

    // 入力欄をクリア
    setInputValue('');

    // --- ここからAI応答のダミー処理 (将来的にAPI連携に置き換える) ---
    // ダミーのAI応答を少し遅れて追加する
    setTimeout(() => {
      const newAiMessage: Message = {
        id: messageIdCounter + 1, // ユーザーメッセージの次のID
        sender: 'ai',
        text: `「${newUserMessage.text}」ですね！(これはダミー応答です)`,
      };
      setMessages(prevMessages => [...prevMessages, newAiMessage]);
      setMessageIdCounter(prevCounter => prevCounter + 2); // AIメッセージの分もIDを進める
    }, 500); // 0.5秒後に応答
     // --- ダミー処理ここまで ---
  };

  /**
   * Enterキーでも送信できるようにする関数
   * @param event キーボードイベント
   */
  const handleKeyDown = (event: React.KeyboardEvent<HTMLInputElement>) => {
    if (event.key === 'Enter') {
      handleSendMessage();
    }
  };


  return (
    <div className="app-container">
      <h1>AIロールプレイチャット (仮)</h1>
      <div className="chat-window">
        {messages.map((msg) => (
          <div key={msg.id} className={`message ${msg.sender}`}>
            <span className="sender-label">{msg.sender === 'user' ? 'あなた' : 'AI'}</span>
            <p className="message-text">{msg.text}</p>
          </div>
        ))}
      </div>
      <div className="input-area">
        <input
          type="text"
          value={inputValue}
          onChange={handleInputChange}
          onKeyDown={handleKeyDown} // Enterキーでの送信を追加
          placeholder="メッセージを入力..."
        />
        <button onClick={handleSendMessage}>送信</button>
      </div>
    </div>
  );
}

export default App;
