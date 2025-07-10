// src/components/ChatInput.tsx
import React, { useRef, useEffect } from 'react'; // useRef, useEffect をインポート
import Button from './Button';
import styles from './ChatInput.module.css';

interface ChatInputProps {
  value: string;
  // ★ onChange は (event) => void から (value: string) => void に変更
  onChange: (value: string) => void; 
  onSendMessage: () => void;
  isLoading: boolean;
  isSendDisabled?: boolean;
}

const ChatInput: React.FC<ChatInputProps> = ({
  value,
  onChange,
  onSendMessage,
  isLoading,
  isSendDisabled = false,
}) => {
  const textareaRef = useRef<HTMLTextAreaElement>(null);

  // ★ 入力値(value)の変更に応じて高さを自動調整する
  useEffect(() => {
    if (textareaRef.current) {
      textareaRef.current.style.height = 'auto'; // 一旦高さをリセット
      const scrollHeight = textareaRef.current.scrollHeight;
      textareaRef.current.style.height = `${scrollHeight}px`; // 計算後の高さを設定
    }
  }, [value]);

  // ★ Enterキーでの送信、Shift+Enterでの改行をハンドリング
  const handleKeyDown = (event: React.KeyboardEvent<HTMLTextAreaElement>) => {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault(); // デフォルトの改行動作をキャンセル
      if (!isLoading && !isSendDisabled && value.trim()) {
        onSendMessage();
      }
    }
  };

  return (
    <div className={styles.inputArea}>
      {/* ★ input を textarea に変更 */}
      <textarea
        ref={textareaRef}
        value={value}
        onChange={(e) => onChange(e.target.value)} // 親コンポーネントには値のみを渡す
        onKeyDown={handleKeyDown}
        placeholder="メッセージを入力 (Shift+Enterで改行)"
        disabled={isLoading}
        className={styles.textarea} // CSSクラス名を変更
        rows={1} // 初期表示は1行
      />
      <Button
        onClick={onSendMessage}
        disabled={isLoading || isSendDisabled || !value.trim()}
        isLoading={isLoading}
        className={styles.sendButton}
      >
        送信
      </Button>
    </div>
  );
};

export default ChatInput;