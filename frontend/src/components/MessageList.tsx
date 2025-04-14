import React, { useRef, useEffect } from 'react';
import MessageItem from './MessageItem';
// Message 型定義をインポート
interface Message {
  id: string;
  sender: 'user' | 'ai';
  text: string;
  imageUrl?: string;
}

// ★ CSS Modules を使う場合
import styles from './MessageList.module.css';

interface MessageListProps {
  messages: Message[];
  isLoading: boolean; // ローディング表示用
}

const MessageList: React.FC<MessageListProps> = ({ messages, isLoading }) => {
  const chatWindowRef = useRef<HTMLDivElement>(null);

  // 自動スクロール処理
  const scrollToBottom = () => {
    if (chatWindowRef.current) {
      chatWindowRef.current.scrollTop = chatWindowRef.current.scrollHeight;
    }
  };

  // messages 配列が更新されるたびに一番下にスクロール
  useEffect(() => {
    scrollToBottom();
  }, [messages]);

  return (
    // ★ CSS Modules を適用
    <div className={styles.chatWindow} ref={chatWindowRef}>
      {messages.map((msg) => (
        <MessageItem key={msg.id} message={msg} />
      ))}
      {/* ローディング表示 */}
      {isLoading && (
        // ★ CSS Modules を適用 (MessageItem と同じスタイルを流用 or 専用スタイル)
        <div className={`${styles.message} ${styles.ai} ${styles.loading}`}>
          <span className={styles.senderLabel}>AI</span>
          <p className={styles.messageText}>考え中...</p>
        </div>
      )}
    </div>
  );
};

export default MessageList;
