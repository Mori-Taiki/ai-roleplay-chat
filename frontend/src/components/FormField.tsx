// import React from "react";
import { UseFormRegister, FieldErrors, Path } from "react-hook-form";
// import { CharacterFormData } from '../models/CharacterFormData'; // 汎用化のため特定の型に依存しないようにする
import styles from "./FormField.module.css"; // 専用のCSS Moduleを作成

// TFormValues をジェネリック型として受け取るように変更
interface FormFieldProps<TFormValues extends Record<string, any>> {
  type: "text" | "textarea" | "url" | "password" | "email" | "number"; // 対応する input type
  name: Path<TFormValues>; // react-hook-form の Path 型を使用
  label: string;
  register: UseFormRegister<TFormValues>;
  errors: FieldErrors<TFormValues>;
  required?: boolean; // ラベル横の * 表示用
  placeholder?: string;
  rows?: number; // textarea用
  maxLength?: number;
  // 他の input/textarea 属性も必要なら追加 (例: autoComplete, pattern など)
  containerClassName?: string; // 外側の div のクラス名を指定可能に
}

const FormField = <TFormValues extends Record<string, any>>({
  type,
  name,
  label,
  register,
  errors,
  required = false,
  placeholder,
  rows,
  maxLength,
  containerClassName = "",
  ...props // 残りの props (autoComplete など) を input/textarea に渡す
}: FormFieldProps<TFormValues>) => {
  // name からエラーメッセージを取得 (ネストされたフィールドも考慮)
  const error = errors[name];
  const hasError = !!error;

  // バリデーションルールを動的に構築 (例)
  const rules = {
    required: required ? `${label}は必須です` : false,
    maxLength: maxLength
      ? {
          value: maxLength,
          message: `${label}は${maxLength}文字以内で入力してください`,
        }
      : undefined,
    // type === 'url' の場合の簡易パターン例 (必要に応じて調整)
    pattern:
      type === "url"
        ? {
            value: /^(https?:\/\/).+/i,
            message: "有効なURL形式で入力してください",
          }
        : undefined,
  };

  // レンダリングする要素を決定
  const InputComponent = type === "textarea" ? "textarea" : "input";

  return (
    <div className={`${styles.formGroup} ${containerClassName}`}>
      <label htmlFor={name} className={styles.label}>
        {label} {required && <span className={styles.required}>*</span>}
      </label>
      <InputComponent
        id={name}
        type={type === "textarea" ? undefined : type} // textarea には type 属性不要
        rows={type === "textarea" ? rows : undefined}
        placeholder={placeholder}
        maxLength={maxLength} // HTML属性としても設定
        // register 関数を適用し、バリデーションルールを渡す
        {...register(name, rules)}
        className={`${type === "textarea" ? styles.textarea : styles.input} ${
          hasError ? styles.inputError : ""
        }`}
        aria-invalid={hasError ? "true" : "false"} // アクセシビリティ対応
        {...props} // 残りの props を展開
      />
      {/* エラーメッセージ表示 */}
      {error && (
        <span className={styles.errorMessage} role="alert">
          {error.message?.toString()}
        </span>
      )}
    </div>
  );
};

export default FormField;
