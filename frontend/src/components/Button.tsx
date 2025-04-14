import { ElementType, ComponentPropsWithoutRef, ReactNode } from 'react';
import styles from './Button.module.css';

const DEFAULT_ELEMENT = 'button';

type ButtonSize = 'sm' | 'md' | 'lg';

// ボタンコンポーネント自身が持つ固有の Props の型
type ButtonOwnProps<E extends ElementType = ElementType> = {
  as?: E; // レンダリングする要素/コンポーネント (例: 'a', 'button', Link)
  variant?: 'primary' | 'secondary' | 'danger' | 'link';
  size?: ButtonSize;
  isLoading?: boolean;
  loadingText?: string;
  children: ReactNode;
  className?: string;
};

// Button コンポーネントが最終的に受け取る Props の型
// ButtonOwnProps と、as で指定された要素/コンポーネントが持つ Props をマージする
// (ただし、ButtonOwnProps と重複するキーは除外)
type ButtonProps<E extends ElementType> = ButtonOwnProps<E> & Omit<ComponentPropsWithoutRef<E>, keyof ButtonOwnProps>;

/**
 * ポリモーフィック対応した汎用ボタンコンポーネント
 * as prop でレンダリングする要素やコンポーネントを指定できます。
 * 例: <Button as="a" href="/home">ホーム</Button>
 * 例: <Button as={Link} to="/profile">プロフィール</Button>
 */
const Button = <E extends ElementType = typeof DEFAULT_ELEMENT>({
  as,
  variant = 'primary',
  size = 'md',
  isLoading = false,
  loadingText = '処理中...',
  children,
  className = '',
  disabled,
  ...props // ここには as で指定された要素固有の props (例: Link なら 'to', 'a' なら 'href') が入る
}: ButtonProps<E>) => {
  // as prop が指定されていればそれを使用し、なければデフォルト ('button') を使用
  const Component = as || DEFAULT_ELEMENT;
  const sizeClass = styles[size] || styles.md;

  const buttonClasses = `
    ${styles.button}
    ${styles[variant]}
    ${sizeClass}
    ${isLoading ? styles.loading : ''}
    ${className}
  `;

  // デフォルトが button 要素の場合、type 属性がなければ 'button' を自動で付与
  // (submit ボタンにしたい場合は type="submit" を明示的に渡す)
  const defaultButtonType = Component === 'button' && !props.type ? 'button' : undefined;

  return (
    <Component
      className={buttonClasses}
      disabled={disabled || isLoading} // ローディング中も disabled にする
      type={defaultButtonType} // デフォルトの type を設定
      {...props} // Link の 'to' や 'a' の 'href' などがここに展開される
    >
      {isLoading ? (
        <>
          <span className={styles.spinner} aria-hidden="true"></span>
          {loadingText}
        </>
      ) : (
        children
      )}
    </Component>
  );
};

export default Button;
