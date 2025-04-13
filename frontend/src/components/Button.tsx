import React, { ButtonHTMLAttributes } from "react";
import styles from "./Button.module.css"; // 専用のCSS Module

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: "primary" | "secondary" | "danger" | "link"; // ボタンの種類
  isLoading?: boolean; // ローディング状態
  loadingText?: string; // ローディング中に表示するテキスト
  children: React.ReactNode; // ボタンのラベルなど
}

const Button: React.FC<ButtonProps> = ({
  variant = "primary", // デフォルトは primary
  isLoading = false,
  loadingText = "処理中...",
  children,
  className = "", // 外部からクラス名を追加可能に
  disabled, // disabled 属性を受け取る
  ...props // 残りの button 属性 (onClick, type など)
}) => {
  const buttonClasses = `
    ${styles.button}
    ${styles[variant]}
    ${isLoading ? styles.loading : ""}
    ${className}
  `;

  return (
    <button
      className={buttonClasses}
      disabled={disabled || isLoading} // ローディング中も非活性化
      {...props}
    >
      {isLoading ? (
        <>
          <span className={styles.spinner} aria-hidden="true"></span>{" "}
          {/* スピナー要素 */}
          {loadingText}
        </>
      ) : (
        children
      )}
    </button>
  );
};

export default Button;
