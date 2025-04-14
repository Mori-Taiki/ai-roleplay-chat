// src/components/ChatInput.tsx
import React from 'react';
import Button from './Button'; // Button コンポーネントをインポート
// import FormField from './FormField'; // FormField を使う場合
// ★ CSS Modules を使う場合
import styles from './ChatInput.module.css';

interface ChatInputProps {
  value: string;
  onChange: (event: React.ChangeEvent<HTMLInputElement>) => void;
  onSendMessage: () => void;
  onGenerateImage: () => void;
  onKeyDown: (event: React.KeyboardEvent<HTMLInputElement>) => void;
  isLoading: boolean;
  isSendDisabled?: boolean; // 送信ボタンを個別制御したい場合 (任意)
  isImageGenDisabled?: boolean; // 画像生成ボタンを個別制御したい場合 (任意)
}

const ChatInput: React.FC<ChatInputProps> = ({
  value,
  onChange,
  onSendMessage,
  onGenerateImage,
  onKeyDown,
  isLoading,
  isSendDisabled = false,
  isImageGenDisabled = false,
}) => {
  return (
    // ★ CSS Modules を適用
    <div className={styles.inputArea}>
      {/* シンプルな input の場合 */}
      <input
        type="text"
        value={value}
        onChange={onChange}
        onKeyDown={onKeyDown}
        placeholder="メッセージを入力..."
        disabled={isLoading}
        className={styles.input} // ★ CSS Modules
      />
      <Button
        onClick={onSendMessage}
        disabled={isLoading || isSendDisabled || !value.trim()} // ローディング中 or 個別無効 or 入力空
        isLoading={isLoading /* 送信専用ローディングを使う場合 isSending */}
        className={styles.sendButton} // ★ CSS Modules
      >
        送信
      </Button>
      <Button
        onClick={onGenerateImage}
        disabled={isLoading || isImageGenDisabled || !value.trim()}
        isLoading={isLoading /* 画像生成専用ローディングを使う場合 isGenerating */}
        variant="secondary" // または他の variant
        className={styles.imageButton} // ★ CSS Modules
      >
        画像生成
      </Button>
    </div>
  );
};

export default ChatInput;
