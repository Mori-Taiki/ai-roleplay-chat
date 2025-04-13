// src/pages/CharacterSetupPage.tsx (雛形)
import React, { useState, useEffect } from "react";
import { useParams, useNavigate, Link } from "react-router-dom"; // React Router のフックをインポート
import { CharacterProfileResponse } from "../models/CharacterProfileResponse"; // データ取得時に使う
import { CreateCharacterProfileRequest } from "../models/CreateCharacterProfileRequest"; // データ送信時に使う
import { UpdateCharacterProfileRequest } from "../models/UpdateCharacterProfileRequest"; // データ送信時に使う

const CharacterSetupPage: React.FC = () => {
  // --- ルーター関連フック ---
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const isEditMode = !!id;
  const characterId = isEditMode ? parseInt(id, 10) : null;

  // --- フォーム入力値の状態管理 ---
  const [name, setName] = useState<string>("");
  const [personality, setPersonality] = useState<string>("");
  const [tone, setTone] = useState<string>("");
  const [backstory, setBackstory] = useState<string>("");
  const [systemPrompt, setSystemPrompt] = useState<string>("");
  const [exampleDialogue, setExampleDialogue] = useState<string>("");
  const [avatarImageUrl, setAvatarImageUrl] = useState<string>("");
  const [isActive, setIsActive] = useState<boolean>(true);
  const [isCustomChecked, setIsCustomChecked] = useState<boolean>(false);

  // --- データ読み込み用の状態管理 ---
  const [isLoading, setIsLoading] = useState<boolean>(false); // ローディング状態
  const [error, setError] = useState<string | null>(null); // エラーメッセージ
  const [isSubmitting, setIsSubmitting] = useState<boolean>(false); // 送信中フラグ
  const [submitError, setSubmitError] = useState<string | null>(null); // 送信エラーメッセージ

  // --- 編集モード時のデータ読み込みロジック ---
  useEffect(() => {
    // 編集モードの場合のみデータを読み込む
    if (isEditMode && characterId) {
      setIsLoading(true); // ローディング開始
      setError(null); // エラーをクリア
      console.log(`編集モード: ID=${characterId} のデータを読み込みます`);

      const fetchCharacterData = async () => {
        try {
          // GET /api/characterprofiles/{id} を呼び出す
          const response = await fetch(
            `https://localhost:7000/api/characterprofiles/${characterId}`
          );

          if (!response.ok) {
            if (response.status === 404) {
              throw new Error(
                `キャラクター (ID: ${characterId}) が見つかりません。`
              );
            } else {
              throw new Error(
                `データの読み込みに失敗しました: ${response.statusText}`
              );
            }
          }

          const data: CharacterProfileResponse = await response.json();

          // 取得したデータでフォームの状態を更新
          setName(data.name);
          setPersonality(data.personality ?? ""); // null の場合は空文字に
          setTone(data.tone ?? "");
          setBackstory(data.backstory ?? "");
          setSystemPrompt(data.systemPrompt ?? "");
          setExampleDialogue(data.exampleDialogue ?? "");
          setAvatarImageUrl(data.avatarImageUrl ?? "");
          setIsActive(data.isActive);
          setIsCustomChecked(data.isSystemPromptCustomized);

          console.log("キャラクターデータの読み込み完了:", data);
        } catch (err) {
          setError(
            err instanceof Error ? err.message : "不明なエラーが発生しました"
          );
          console.error("Error fetching character data:", err);
          // エラー発生時はフォームを初期状態にするか、あるいはそのままにするか検討
          // setName(''); setPersonality(''); ... etc.
        } finally {
          setIsLoading(false); // ローディング完了
        }
      };

      fetchCharacterData();
    }
    // 新規作成モードの場合は何もしない (または初期値をリセットする)
    // else {
    //   setName(''); // ... 他の state もリセット
    // }
  }, [characterId, isEditMode]);

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault(); // デフォルトのフォーム送信を抑制
    setIsSubmitting(true); // 送信開始
    setSubmitError(null); // エラーをクリア

    // --- TODO: ここでクライアントサイドバリデーションを追加する (任意) ---
    // 例: if (name.length === 0) { setSubmitError("名前は必須です"); setIsSubmitting(false); return; }

    // --- TODO: ExampleDialogue の state を JSON 文字列に変換する ---
    // もし exampleDialogue state がオブジェクトの配列なら、ここで JSON.stringify() する
    // 現在は string state なので、そのまま使う想定
    const dialogueJsonString = exampleDialogue; // 仮

    try {
      let response: Response;
      const apiUrl = "https://localhost:7000/api/characterprofiles";

      if (isEditMode && characterId) {
        // --- 更新 (PUT) ---
        const requestData: UpdateCharacterProfileRequest = {
          // UpdateCharacterProfileRequest に対応するオブジェクト
          name,
          personality,
          tone,
          backstory,
          systemPrompt: systemPrompt || null,
          exampleDialogue: dialogueJsonString || null,
          avatarImageUrl: avatarImageUrl || null,
          isActive,
          isSystemPromptCustomized: isCustomChecked,
        };
        console.log("Updating character:", characterId, requestData);

        response = await fetch(`${apiUrl}/${characterId}`, {
          method: "PUT",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify(requestData),
        });

        if (!response.ok) {
          // 404 Not Found や 400 Bad Request など
          const errorData = await response.json().catch(() => ({
            message: `更新に失敗しました (${response.status})`,
          }));
          throw new Error(
            errorData.message || `更新に失敗しました (${response.status})`
          );
        }

        alert("キャラクター情報を更新しました！");
      } else {
        // --- 新規登録 (POST) ---
        const requestData: CreateCharacterProfileRequest = {
          // CreateCharacterProfileRequest に対応するオブジェクト
          name,
          personality,
          tone,
          backstory,
          systemPrompt: isCustomChecked ? systemPrompt || null : null,
          exampleDialogue: dialogueJsonString || null,
          avatarImageUrl: avatarImageUrl || null,
          isActive,
        };
        console.log("Creating new character:", requestData); // デバッグ用ログ

        response = await fetch(apiUrl, {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify(requestData),
        });

        if (!response.ok) {
          // 400 Bad Request など
          const errorData = await response.json().catch(() => ({
            message: `登録に失敗しました (${response.status})`,
          }));
          throw new Error(
            errorData.message || `登録に失敗しました (${response.status})`
          );
        }

        // 登録成功時の処理 (レスポンスから新しいIDを取得して編集画面へリダイレクト)
        const createdCharacter: CharacterProfileResponse =
          await response.json(); // サーバーが返すDTOを受け取る
        alert(`キャラクター「${createdCharacter.name}」を登録しました！`);
        navigate(`/characters/edit/${createdCharacter.id}`); // 作成されたキャラクターの編集画面へ
      }
    } catch (err) {
      // API呼び出し中のエラーや、レスポンス処理中のエラー
      const errorMessage =
        err instanceof Error ? err.message : "不明なエラーが発生しました。";
      setSubmitError(`エラー: ${errorMessage}`);
      console.error("Form submission error:", err);
    } finally {
      setIsSubmitting(false); // 送信完了 (成功・失敗問わず)
    }
  };

  // --- TODO: 削除処理 (編集モード時) ---
  const handleDelete = async () => {
    if (!isEditMode || !id) return;
    if (window.confirm(`キャラクター「${name}」を本当に削除しますか？`)) {
      console.log(`キャラクター ID=${id} を削除します (未実装)`);
      // TODO: DELETE /api/characterprofiles/:id を呼び出す
      // 成功したら navigate('/characters'); などで一覧に戻る
    }
  };

  // --- レンダリング ---
  // ローディング表示を追加
  if (isLoading) {
    return <div>データを読み込み中...</div>;
  }
  // エラー表示を追加 (エラーがあればフォームを表示しないなども検討可)
  if (error) {
    return (
      <div style={{ color: "red" }}>
        エラー: {error} <Link to="/characters">一覧に戻る</Link>
      </div>
    );
  }

  return (
    <div>
      <h1>
        {isEditMode ? `キャラクター編集 (ID: ${id})` : "新規キャラクター作成"}
      </h1>

      <form onSubmit={handleSubmit}>
        {/* 各フォームフィールド */}
        <div style={styles.formGroup}>
          <label htmlFor="name">
            名前 <span style={styles.required}>*</span>:
          </label>
          <input
            type="text"
            id="name"
            value={name}
            onChange={(e) => setName(e.target.value)}
            required
            maxLength={30} // バリデーション（サーバー側とも合わせる）
            style={styles.input}
          />
        </div>

        <div style={styles.formGroup}>
          <label htmlFor="personality">
            性格 <span style={styles.required}>*</span>:
          </label>
          <textarea
            id="personality"
            value={personality}
            onChange={(e) => setPersonality(e.target.value)}
            required
            rows={3}
            style={styles.textarea}
          />
        </div>

        <div style={styles.formGroup}>
          <label htmlFor="tone">
            口調 <span style={styles.required}>*</span>:
          </label>
          <textarea
            id="tone"
            value={tone}
            onChange={(e) => setTone(e.target.value)}
            required
            rows={3}
            style={styles.textarea}
          />
        </div>

        <div style={styles.formGroup}>
          <label htmlFor="backstory">
            背景 <span style={styles.required}>*</span>:
          </label>
          <textarea
            id="backstory"
            value={backstory}
            onChange={(e) => setBackstory(e.target.value)}
            required
            rows={5}
            style={styles.textarea}
          />
        </div>

        <div style={styles.formGroup}>
          <label htmlFor="systemPrompt">システムプロンプト (任意):</label>
          <div
            style={{
              ...styles.formGroup,
              flexDirection: "row",
              alignItems: "center",
              marginBottom: "0.5rem",
            }}
          >
            {" "}
            <input
              type="checkbox"
              id="isCustomChecked"
              checked={isCustomChecked}
              onChange={(e) => setIsCustomChecked(e.target.checked)}
              style={{ marginRight: "0.5rem", cursor: "pointer" }} // クリックしやすく
            />
            {/* ラベルもクリック可能にするため htmlFor を使用 */}
            <label htmlFor="isCustomChecked" style={{ cursor: "pointer" }}>
              システムプロンプトをカスタムする
            </label>
          </div>
          <textarea
            id="systemPrompt"
            value={systemPrompt}
            onChange={(e) => setSystemPrompt(e.target.value)}
            rows={5}
            placeholder="入力しない場合は性格などから自動生成されます"
            style={styles.textarea}
            disabled={!isCustomChecked}
          />
        </div>

        {/* TODO: ExampleDialogue の動的入力 UI をここに実装 */}
        <div style={styles.formGroup}>
          <label htmlFor="exampleDialogue">会話例 (任意, JSON形式):</label>
          <textarea
            id="exampleDialogue"
            value={exampleDialogue}
            onChange={(e) => setExampleDialogue(e.target.value)}
            rows={5}
            placeholder='例: [{"user":"入力例","model":"応答例"}]'
            style={styles.textarea}
          />
          <small>将来的には専用UIにします。</small>
        </div>

        <div style={styles.formGroup}>
          <label htmlFor="avatarImageUrl">アバター画像URL (任意):</label>
          <input
            type="url"
            id="avatarImageUrl"
            value={avatarImageUrl}
            onChange={(e) => setAvatarImageUrl(e.target.value)}
            style={styles.input}
          />
        </div>

        <div style={styles.formGroup}>
          <label htmlFor="isActive">
            <input
              type="checkbox"
              id="isActive"
              checked={isActive}
              onChange={(e) => setIsActive(e.target.checked)}
              style={{ marginRight: "0.5rem" }}
            />
            有効なキャラクター
          </label>
        </div>

        {/* 送信ボタンなど */}
        <div style={styles.buttonGroup}>
          <button type="submit" style={styles.button}>
            {isEditMode ? "更新" : "登録"}
          </button>
          {/* 編集モードの場合のみ削除ボタンを表示 */}
          {isEditMode && (
            <button
              type="button"
              onClick={handleDelete}
              style={{ ...styles.button, ...styles.deleteButton }}
            >
              削除
            </button>
          )}
          {/* TODO: 保存後に有効化する「会話する」ボタン */}
          {isEditMode && (
            <button
              type="button"
              disabled
              /* onClick={handleStartChat} */ style={{
                ...styles.button,
                marginLeft: "1rem",
              }}
            >
              会話する (未実装)
            </button>
          )}
          <Link to="/characters" style={{ marginLeft: "1rem" }}>
            キャンセル
          </Link>
        </div>
      </form>
    </div>
  );
};

export default CharacterSetupPage;

// 簡単なインラインスタイル (実際は CSS ファイルや UI ライブラリを使うことを推奨)
const styles = {
  formGroup: {
    marginBottom: "1rem",
    display: "flex",
    flexDirection: "column",
  } as React.CSSProperties,
  label: {
    marginBottom: "0.5rem",
    fontWeight: "bold",
  } as React.CSSProperties,
  input: {
    padding: "0.5rem",
    border: "1px solid #ccc",
    borderRadius: "4px",
  } as React.CSSProperties,
  textarea: {
    padding: "0.5rem",
    border: "1px solid #ccc",
    borderRadius: "4px",
    minHeight: "60px",
    fontFamily: "inherit", // フォントを他の入力と合わせる
  } as React.CSSProperties,
  required: {
    color: "red",
    marginLeft: "0.25rem",
  } as React.CSSProperties,
  buttonGroup: {
    marginTop: "1.5rem",
  } as React.CSSProperties,
  button: {
    padding: "0.75rem 1.5rem",
    border: "none",
    borderRadius: "4px",
    cursor: "pointer",
    backgroundColor: "#007bff",
    color: "white",
    marginRight: "0.5rem",
  } as React.CSSProperties,
  deleteButton: {
    backgroundColor: "#dc3545",
  } as React.CSSProperties,
};
