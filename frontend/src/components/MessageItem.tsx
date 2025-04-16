// src/components/MessageItem.tsx
import React from 'react';
// Message 型定義は src/models/Message.ts などに移動してインポート推奨
interface Message {
  id: string;
  sender: 'user' | 'ai';
  text: string;
  imageUrl?: string;
}

interface MessageItemProps {
  characterName: string;
  message: Message;
}

// ★ CSS Modules を使う場合
import styles from './MessageItem.module.css';

const MessageItem: React.FC<MessageItemProps> = ({ characterName, message }) => {
  return (
    // ★ CSS Modules を適用
    <div key={message.id} className={`${styles.message} ${styles[message.sender]}`}>
      <span className={styles.senderLabel}>{message.sender === 'user' ? '' : characterName}</span>
      {/* 画像URLがあれば画像を表示 */}
      {message.imageUrl && (
        <img
          src={message.imageUrl}
          alt="生成された画像"
          className={styles.image} // ★ CSS Modules
        />
      )}
      {/* テキストがあればテキストを表示 */}
      {message.text && (
        <p className={styles.messageText}>{message.text}</p> // ★ CSS Modules
      )}
    </div>
  );
};

export default MessageItem;
