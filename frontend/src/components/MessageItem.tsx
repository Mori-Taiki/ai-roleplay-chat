import React, { useState } from 'react';
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
  isLatestAiMessage: boolean;
  onGenerateImage: (messageId: string) => void; // ★ 追加
  onEditMessage: (messageId: string, newText: string) => void;
  onRegenerateAi: (messageId: string) => void;
}

const MessageItem: React.FC<MessageItemProps> = ({ 
  characterName, 
  message, 
  onRetry, 
  isLatestUserMessage, 
  isLatestAiMessage,
  onGenerateImage,
  onEditMessage,
  onRegenerateAi 
}) => {
  const [isEditing, setIsEditing] = useState(false);
  const [editText, setEditText] = useState(message.text);

  // ★ 画像生成ボタンを表示すべきかどうかの条件
  const showImageGenButton = message.sender === 'ai' && !message.imageUrl && !message.isImageLoading;

  // 再試行ボタンは最新のユーザーメッセージかつエラーがある場合のみ表示
  const showRetryButton = message.sender === 'user' && isLatestUserMessage && message.isError;
  
  // 編集ボタンは最新のユーザーメッセージかつエラーがない場合（送信成功済み）のみ表示
  const showEditButton = message.sender === 'user' && isLatestUserMessage && !message.isError && !isEditing;

  // 再生成ボタンは最新のAIメッセージのみ表示
  const showRegenerateButton = message.sender === 'ai' && isLatestAiMessage;

  const handleEditSave = () => {
    if (editText.trim() && editText !== message.text) {
      onEditMessage(message.id, editText.trim());
    }
    setIsEditing(false);
  };

  const handleEditCancel = () => {
    setEditText(message.text);
    setIsEditing(false);
  };

  return (
    <div key={message.id} className={`${styles.message} ${styles[message.sender]}`}>
      <span className={styles.senderLabel}>{message.sender === 'user' ? '' : characterName}</span>
      
      <div className={styles.messageContentWrapper}>
        {isEditing ? (
          <div className={styles.editContainer}>
            <textarea
              value={editText}
              onChange={(e) => setEditText(e.target.value)}
              className={styles.editTextarea}
              rows={3}
            />
            <div className={styles.editButtons}>
              <Button
                onClick={handleEditSave}
                variant="primary"
                size="sm"
                disabled={!editText.trim()}
              >
                保存
              </Button>
              <Button
                onClick={handleEditCancel}
                variant="secondary"
                size="sm"
              >
                キャンセル
              </Button>
            </div>
          </div>
        ) : (
          <>
            {message.text && (
              <p className={styles.messageText}>{message.text}</p>
            )}
            
            <div className={styles.messageActions}>
              {showRetryButton && (
                <Button
                  onClick={() => onRetry(message.text)}
                  variant="secondary"
                  size="sm"
                  className={styles.actionButton}
                  title="同じ内容で再送信"
                >
                  再試行
                </Button>
              )}
              
              {showEditButton && (
                <Button
                  onClick={() => setIsEditing(true)}
                  variant="secondary"
                  size="sm"
                  className={styles.actionButton}
                  title="メッセージを編集"
                >
                  編集
                </Button>
              )}

              {showRegenerateButton && (
                <Button
                  onClick={() => onRegenerateAi(message.id)}
                  variant="secondary"
                  size="sm"
                  className={styles.actionButton}
                  title="AI応答を再生成"
                >
                  再生成
                </Button>
              )}
            </div>
          </>
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