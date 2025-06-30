import React from 'react';
import { Message } from '../models/Message';
import styles from './MessageItem.module.css';
import Button from './Button';

interface MessageItemProps {
  characterName: string;
  message: Message;
  onRetry: (prompt: string) => void;
  isLatestUserMessage: boolean;
}

const MessageItem: React.FC<MessageItemProps> = ({ characterName, message, onRetry, isLatestUserMessage }) => {
  return (
    <div key={message.id} className={`${styles.message} ${styles[message.sender]}`}>
      <span className={styles.senderLabel}>{message.sender === 'user' ? '' : characterName}</span>
      
      <div className={styles.messageContentWrapper}>
        {message.text && (
          <p className={styles.messageText}>{message.text}</p>
        )}
        
        {message.sender === 'user' && isLatestUserMessage && (
          <Button
            onClick={() => onRetry(message.text)}
            variant="secondary"
            size="sm"
            className={styles.retryButton}
            title="同じ内容で再送信"
          >
            再試行
          </Button>
        )}
      </div>

      {message.imageUrl && (
        <img
          src={message.imageUrl}
          alt="生成された画像"
          className={styles.image}
        />
      )}
    </div>
  );
};

export default MessageItem;