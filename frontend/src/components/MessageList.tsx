import React, { useRef, useEffect } from 'react';
import MessageItem from './MessageItem';
import { Message } from '../models/Message';
import styles from './MessageList.module.css';

interface MessageListProps {
  characterName: string;
  messages: Message[];
  isLoading: boolean;
  onRetry: (prompt: string) => void;
}

const MessageList: React.FC<MessageListProps> = ({ characterName, messages, isLoading, onRetry }) => {
  const chatWindowRef = useRef<HTMLDivElement>(null);

  const scrollToBottom = () => {
    if (chatWindowRef.current) {
      chatWindowRef.current.scrollTop = chatWindowRef.current.scrollHeight;
    }
  };

  useEffect(() => {
    scrollToBottom();
  }, [messages]);

  // 描画のたびに最新のユーザーメッセージのインデックスを算出
  const lastUserMessageIndex = messages.map(m => m.sender).lastIndexOf('user');

  return (
    <div className={styles.chatWindow} ref={chatWindowRef}>
      {messages.map((msg, index) => {
        // 自分が最新のユーザーメッセージか判定
        const isLatestUserMessage = msg.sender === 'user' && index === lastUserMessageIndex;
        return (
          <MessageItem
            key={msg.id}
            characterName={characterName}
            message={msg}
            onRetry={onRetry}
            isLatestUserMessage={isLatestUserMessage}
          />
        );
      })}
      {isLoading && (
        <div className={`${styles.message} ${styles.ai} ${styles.loading}`}>
          <span className={styles.senderLabel}>{characterName}</span>
          <p className={styles.messageText}>考え中...</p>
        </div>
      )}
    </div>
  );
};

export default MessageList;