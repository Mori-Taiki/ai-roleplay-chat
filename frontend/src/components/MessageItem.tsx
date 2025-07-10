import React from 'react';
import { Message } from '../models/Message';
import styles from './MessageItem.module.css';
import Button from './Button';

interface DisplayMessage extends Message {
  isImageLoading?: boolean;
}
interface MessageItemProps {
  characterName: string;
  message: DisplayMessage;
  onRetry: (prompt: string) => void;
  isLatestUserMessage: boolean;
  onGenerateImage: (messageId: string) => void; // ★ 追加
}

const MessageItem: React.FC<MessageItemProps> = ({ characterName, message, onRetry, isLatestUserMessage, onGenerateImage }) => {
  // ★ 画像生成ボタンを表示すべきかどうかの条件
  const showImageGenButton = message.sender === 'ai' && !message.imageUrl && !message.isImageLoading;

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

      {message.isImageLoading && !message.imageUrl && (
        <div className={styles.imageLoading}>
          <div className={styles.spinner}></div>
          <span>画像生成中...</span>
        </div>
      )}

      {/* ★ 画像生成ボタンの追加 */}
      {showImageGenButton && (
        <div style={{ marginTop: '8px' }}>
          <Button
            onClick={() => onGenerateImage(message.id)}
            variant="secondary"
            size="sm"
            title="このメッセージから画像を生成する"
          >
            画像生成
          </Button>
        </div>
      )}
    </div>
  );
};

export default MessageItem;