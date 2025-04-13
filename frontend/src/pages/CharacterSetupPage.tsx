// src/pages/CharacterSetupPage.tsx (抜粋)
import React, { useEffect } from "react";
import { useParams, useNavigate, Link } from "react-router-dom";
import {
  useForm,
  SubmitHandler,
  Controller,
  useFieldArray,
} from "react-hook-form"; // react-hook-form をインポート
import { CreateCharacterProfileRequest } from "../models/CreateCharacterProfileRequest";
import { UpdateCharacterProfileRequest } from "../models/UpdateCharacterProfileRequest";
import { useCharacterProfile } from "../hooks/useCharacterProfile"; // カスタムフック
import styles from "./CharacterSetupPage.module.css";
// import { getGenericErrorMessage } from '../utils/errorHandler'; // 必要に応じて

interface DialoguePairForm {
  user: string;
  model: string;
}

interface CharacterFormData {
  name: string;
  personality: string;
  tone: string;
  backstory: string;
  systemPrompt: string | null;
  isSystemPromptCustomized: boolean;
  // exampleDialogue: string | null; // useFieldArray 導入時に DialoguePair[] 型に変更
  avatarImageUrl: string | null;
  isActive: boolean;
  dialoguePairs: DialoguePairForm[];
}

const CharacterSetupPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const isEditMode = !!id;
  const characterId = isEditMode ? parseInt(id, 10) : null;

  // カスタムフックから API 関連の機能を取得
  const {
    character: initialCharacterData, // 初期データとして取得
    isLoading: isLoadingCharacter, // ローディング状態
    error: fetchError,
    isSubmitting: isApiSubmitting, // API 通信中の状態
    submitError: apiSubmitError,
    fetchCharacter,
    createCharacter,
    updateCharacter,
    deleteCharacter,
  } = useCharacterProfile();

  // --- react-hook-form の設定 ---
  const {
    register, // 入力要素を登録する関数
    handleSubmit, // フォーム送信を処理する関数
    control, // Controller コンポーネントで使用 (後述)
    reset, // フォーム値をリセット/初期化する関数
    watch, // 特定のフォーム値を監視する関数 (isSystemPromptCustomized で使用)
    formState: { errors, isSubmitting: isFormSubmitting }, // フォームの状態 (エラー、送信中かなど)
  } = useForm<CharacterFormData>({
    // デフォルト値を設定
    defaultValues: {
      name: "",
      personality: "",
      tone: "",
      backstory: "",
      systemPrompt: null,
      isSystemPromptCustomized: false,
      // exampleDialogue: null, // useFieldArray で管理
      avatarImageUrl: null,
      isActive: true,
      dialoguePairs: [], // useFieldArray 用
    },
  });

  // --- useFieldArray の設定 ---
  const { fields, append, remove } = useFieldArray({
    control, // useForm の control を渡す
    name: "dialoguePairs", // 管理するフィールド配列の名前
  });

  // isSystemPromptCustomized の値を監視
  const isCustomChecked = watch("isSystemPromptCustomized");

  // --- 編集モード時のデータ読み込みとフォームへの反映 ---
  useEffect(() => {
    if (isEditMode && characterId) {
      fetchCharacter(characterId); // カスタムフックでデータ取得
    }
    // 新規作成モードの場合や、依存配列の characterId が変わったときに
    // フォームをデフォルト値でリセットする（必要に応じて）
    else {
      reset(); // defaultValues でリセット
    }
  }, [characterId, isEditMode, fetchCharacter]); // fetchCharacter も依存配列に追加

  // データ取得が完了したらフォームに値をセット
  useEffect(() => {
    if (initialCharacterData) {
      // dialoguePairs の処理を追加する必要あり
      let parsedPairs = [];
      if (initialCharacterData.exampleDialogue) {
        try {
          parsedPairs = JSON.parse(initialCharacterData.exampleDialogue);
          if (!Array.isArray(parsedPairs)) parsedPairs = [];
        } catch {
          parsedPairs = [];
        }
      }

      reset({
        // react-hook-form の reset 関数でフォーム値を更新
        name: initialCharacterData.name,
        personality: initialCharacterData.personality ?? "",
        tone: initialCharacterData.tone ?? "",
        backstory: initialCharacterData.backstory ?? "",
        systemPrompt: initialCharacterData.systemPrompt ?? null,
        isSystemPromptCustomized: initialCharacterData.isSystemPromptCustomized,
        avatarImageUrl: initialCharacterData.avatarImageUrl ?? null,
        isActive: initialCharacterData.isActive,
        dialoguePairs: parsedPairs.map((p) => ({
          user: p.user ?? "",
          model: p.model ?? "",
        })), // useFieldArray 用
      });
    }
  }, [initialCharacterData, reset]); // reset も依存配列に追加推奨

  // --- フォーム送信処理 ---
  const onSubmit: SubmitHandler<CharacterFormData> = async (formData) => {
    // formData には react-hook-form が収集したフォームデータが入る
    console.log("Form Data:", formData); // デバッグ用

    // exampleDialogue を JSON 文字列に変換 (useFieldArray 導入後は formData.dialoguePairs を使う)
    let dialogueJsonString: string | null = null;
    try {
      if (formData.dialoguePairs && formData.dialoguePairs.length > 0) {
        // id を除外する処理は不要になる (useFieldArray が管理)
        dialogueJsonString = JSON.stringify(formData.dialoguePairs);
      }
    } catch (stringifyError) {
      console.error("Failed to stringify dialogue pairs:", stringifyError);
      // setError("dialoguePairs", { type: "manual", message: "会話例の保存形式への変換に失敗しました。" }); // react-hook-form のエラーセット
      alert("会話例データの保存形式への変換に失敗しました。"); // 一時的なアラート
      return;
    }

    if (isEditMode && characterId) {
      // --- 更新処理 ---
      const requestData: UpdateCharacterProfileRequest = {
        name: formData.name,
        personality: formData.personality,
        tone: formData.tone,
        backstory: formData.backstory,
        // isCustomChecked が false なら systemPrompt は送らない (バックエンドで自動生成)
        systemPrompt: formData.isSystemPromptCustomized
          ? formData.systemPrompt
          : null,
        isSystemPromptCustomized: formData.isSystemPromptCustomized,
        exampleDialogue: dialogueJsonString,
        avatarImageUrl: formData.avatarImageUrl,
        isActive: formData.isActive,
      };
      const success = await updateCharacter(characterId, requestData); // カスタムフック呼び出し
      if (success) {
        alert("キャラクター情報を更新しました！");
        // 必要なら一覧ページへリダイレクトなど
        // navigate('/characters');
      }
    } else {
      // --- 新規作成処理 ---
      const requestData: CreateCharacterProfileRequest = {
        name: formData.name,
        personality: formData.personality,
        tone: formData.tone,
        backstory: formData.backstory,
        systemPrompt: formData.isSystemPromptCustomized
          ? formData.systemPrompt
          : null, // カスタムする場合のみ送信
        // isSystemPromptCustomized は Create リクエストには不要 (バックエンドで判定)
        exampleDialogue: dialogueJsonString,
        avatarImageUrl: formData.avatarImageUrl,
        isActive: formData.isActive, // デフォルト true だが念のため
      };
      const createdCharacter = await createCharacter(requestData); // カスタムフック呼び出し
      if (createdCharacter) {
        alert(`キャラクター「${createdCharacter.name}」を登録しました！`);
        navigate(`/characters/edit/${createdCharacter.id}`); // 作成されたキャラクターの編集画面へ
      }
    }
  };

  // --- 削除処理 ---
  const handleDeleteClick = async () => {
    if (!isEditMode || !characterId) return;
    if (
      window.confirm(
        `キャラクター「${
          initialCharacterData?.name || "未名のキャラクター"
        }」(ID: ${characterId}) を本当に削除しますか？`
      )
    ) {
      const success = await deleteCharacter(characterId); // カスタムフック呼び出し
      if (success) {
        alert(`キャラクター (ID: ${characterId}) を削除しました。`);
        navigate("/characters");
      }
    }
  };

  // --- レンダリング ---
  if (isLoadingCharacter) {
    return <div>データを読み込み中...</div>;
  }
  if (fetchError) {
    return (
      <div style={{ color: "red" }}>
        エラー: {fetchError} <Link to="/characters">一覧に戻る</Link>
      </div>
    );
  }

  // isFormSubmitting と isApiSubmitting の両方を考慮してボタンを無効化
  const isProcessing = isFormSubmitting || isApiSubmitting;

  return (
    <div>
      <h1>
        {isEditMode ? `キャラクター編集 (ID: ${id})` : "新規キャラクター作成"}
      </h1>

      {/* handleSubmit で onSubmit 関数をラップ */}
      <form onSubmit={handleSubmit(onSubmit)}>
        {/* 名前フィールド */}
        <div className={styles.formGroup}>
          <label htmlFor="name">
            名前 <span className={styles.required}>*</span>:
          </label>
          <input
            type="text"
            id="name"
            // react-hook-form の register を使用
            {...register("name", {
              required: "名前は必須です", // バリデーションルール
              maxLength: {
                value: 30,
                message: "名前は30文字以内で入力してください",
              },
            })}
            maxLength={30} // HTML の maxLength も残しておくと入力制限がかかる
            className={`${styles.input} ${
              errors.name ? styles.inputError : ""
            }`} // エラー時にスタイル変更
          />
          {/* エラーメッセージ表示 */}
          {errors.name && (
            <span className={styles.errorMessage}>{errors.name.message}</span>
          )}
        </div>

        {/* Personality フィールド (同様に register とエラー表示を追加) */}
        <div className={styles.formGroup}>
          <label htmlFor="personality">
            性格 <span className={styles.required}>*</span>:
          </label>
          <textarea
            id="personality"
            {...register("personality", { required: "性格は必須です" })}
            rows={3}
            className={`${styles.textarea} ${
              errors.personality ? styles.inputError : ""
            }`}
          />
          {errors.personality && (
            <span className={styles.errorMessage}>
              {errors.personality.message}
            </span>
          )}
        </div>

        {/* Tone フィールド (同様に) */}
        <div className={styles.formGroup}>
          <label htmlFor="tone">
            口調 <span className={styles.required}>*</span>:
          </label>
          <textarea
            id="tone"
            {...register("tone", { required: "口調は必須です" })}
            rows={3}
            className={`${styles.textarea} ${
              errors.tone ? styles.inputError : ""
            }`}
          />
          {errors.tone && (
            <span className={styles.errorMessage}>{errors.tone.message}</span>
          )}
        </div>

        {/* Backstory フィールド (同様に) */}
        <div className={styles.formGroup}>
          <label htmlFor="backstory">
            背景 <span className={styles.required}>*</span>:
          </label>
          <textarea
            id="backstory"
            {...register("backstory", { required: "背景は必須です" })}
            rows={5}
            className={`${styles.textarea} ${
              errors.backstory ? styles.inputError : ""
            }`}
          />
          {errors.backstory && (
            <span className={styles.errorMessage}>
              {errors.backstory.message}
            </span>
          )}
        </div>

        {/* --- System Prompt --- */}
        <div className={styles.formGroup}>
          <label htmlFor="systemPrompt" className={styles.label}>
            システムプロンプト ({isCustomChecked ? "カスタム入力" : "自動生成"}
            ):
          </label>
          <div /* Checkbox wrapper */>
            {/* チェックボックスも register で管理 */}
            <input
              type="checkbox"
              id="isSystemPromptCustomized"
              {...register("isSystemPromptCustomized")} // バリデーション不要ならこれだけ
              style={{ marginRight: "0.5rem", cursor: "pointer" }}
            />
            <label
              htmlFor="isSystemPromptCustomized"
              style={{ cursor: "pointer" }}
            >
              システムプロンプトをカスタムする
            </label>
          </div>
          <textarea
            id="systemPrompt"
            {...register("systemPrompt", {
              // isCustomChecked が true の場合のみ必須にするなどの条件付きバリデーションも可能
              required: isCustomChecked
                ? "カスタムプロンプトは必須です"
                : false,
            })}
            rows={5}
            placeholder={
              isCustomChecked ? "カスタムプロンプトを入力" : "自動生成されます"
            }
            className={styles.textarea}
            disabled={!isCustomChecked} // 監視している値で無効化
          />
          {/* エラー表示 (必要なら) */}
          {errors.systemPrompt && (
            <span className={styles.errorMessage}>
              {errors.systemPrompt.message}
            </span>
          )}
          {!isCustomChecked && (
            <small className={styles.hintText}>
              カスタムチェックを入れると編集できます。
            </small>
          )}
        </div>

        <div className={styles.formGroup}>
          <label className={styles.label}>会話例 (任意、0～10件):</label>
          {fields.map((field, index) => (
            // key には field.id を使用 (react-hook-form が生成)
            <div key={field.id} className={styles.pairContainer}>
              {/* ユーザー発言 */}
              <div style={{ marginRight: "1rem", flexGrow: 1 }}>
                <label
                  htmlFor={`dialoguePairs.${index}.user`}
                  className={styles.subLabel}
                >
                  ユーザー発言 {index + 1}:
                </label>
                <textarea
                  id={`dialoguePairs.${index}.user`}
                  // register でフィールドを登録 (名前はインデックス付き)
                  {...register(`dialoguePairs.${index}.user`, {
                    // 必要ならバリデーションルールを追加
                    required: "ユーザー発言は必須です",
                  })}
                  rows={2}
                  className={`${styles.textarea} ${
                    errors.dialoguePairs?.[index]?.user ? styles.inputError : ""
                  }`}
                />
                {/* エラー表示 */}
                {errors.dialoguePairs?.[index]?.user && (
                  <span className={styles.errorMessage}>
                    {errors.dialoguePairs[index]?.user?.message}
                  </span>
                )}
              </div>
              {/* モデル応答 */}
              <div style={{ flexGrow: 1 }}>
                <label
                  htmlFor={`dialoguePairs.${index}.model`}
                  className={styles.subLabel}
                >
                  モデル応答 {index + 1}:
                </label>
                <textarea
                  id={`dialoguePairs.${index}.model`}
                  {...register(`dialoguePairs.${index}.model`, {
                    required: "モデル応答は必須です",
                  })}
                  rows={2}
                  className={`${styles.textarea} ${
                    errors.dialoguePairs?.[index]?.model
                      ? styles.inputError
                      : ""
                  }`}
                />
                {errors.dialoguePairs?.[index]?.model && (
                  <span className={styles.errorMessage}>
                    {errors.dialoguePairs[index]?.model?.message}
                  </span>
                )}
              </div>
              {/* 削除ボタン (remove 関数を使用) */}
              <button
                type="button"
                onClick={() => remove(index)}
                className={styles.deletePairButton}
                title="この会話例を削除"
                disabled={isProcessing} // 処理中は無効化
              >
                ×
              </button>
            </div>
          ))}

          {/* 追加ボタン (append 関数を使用) */}
          {/* 件数制限 (fields.length で判定) */}
          {fields.length < 10 && (
            <button
              type="button"
              onClick={() => append({ user: "", model: "" })}
              className={styles.addPairButton}
              disabled={isProcessing} // 処理中は無効化
            >
              会話例を追加
            </button>
          )}
          <small style={{ display: "block", marginTop: "0.5rem" }}>
            AIの応答スタイルを具体的に示す会話例を入力します。
          </small>
        </div>

        <div className={styles.formGroup}>
          <label htmlFor="avatarImageUrl">アバター画像URL (任意):</label>
          <input
            type="url" // type="url" は簡易的なURL形式チェックを行う
            id="avatarImageUrl"
            {...register("avatarImageUrl", {
              pattern: {
                value: /^(https?:\/\/).*/,
                message: "有効なURLを入力してください",
              },
            })}
            className={`${styles.input} ${
              errors.avatarImageUrl ? styles.inputError : ""
            }`}
          />
          {errors.avatarImageUrl && (
            <span className={styles.errorMessage}>
              {errors.avatarImageUrl.message}
            </span>
          )}
        </div>

        {/* --- Is Active --- */}
        <div className={styles.formGroup}>
          <label htmlFor="isActive">
            <input
              type="checkbox"
              id="isActive"
              {...register("isActive")}
              style={{ marginRight: "0.5rem" }}
            />
            有効なキャラクター
          </label>
        </div>

        {/* --- 送信ボタン・エラー表示 --- */}
        <div className={styles.buttonGroup}>
          {/* API 送信時のエラー表示 */}
          {apiSubmitError && (
            <div
              className={styles.errorMessage}
              style={{ marginBottom: "1rem" }}
            >
              {apiSubmitError}
            </div>
          )}

          <button
            type="submit"
            className={styles.button}
            disabled={isProcessing} // フォーム送信中 or API通信中は無効化
          >
            {isProcessing ? "処理中..." : isEditMode ? "更新" : "登録"}
          </button>
          {isEditMode && (
            <button
              type="button"
              onClick={handleDeleteClick} // 削除処理を実行
              className={`${styles.button} ${styles.deleteButton}`}
              disabled={isProcessing} // 処理中は無効化
            >
              削除
            </button>
          )}
          {/* ... 他のボタン ... */}
          <Link to="/characters" style={{ marginLeft: "1rem" }}>
            キャンセル
          </Link>
        </div>
      </form>
    </div>
  );
};

export default CharacterSetupPage;
