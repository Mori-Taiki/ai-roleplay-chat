import React, { useState, useEffect } from "react";
import { useParams, useNavigate, Link } from "react-router-dom";
import { CharacterProfileResponse } from "../models/CharacterProfileResponse";
import { CreateCharacterProfileRequest } from "../models/CreateCharacterProfileRequest";
import { UpdateCharacterProfileRequest } from "../models/UpdateCharacterProfileRequest";
import styles from "./CharacterSetupPage.module.css";

interface DialoguePair {
  id: number; // リスト内で各ペアを一意に識別するための一時的なID
  user: string;
  model: string;
}

const CharacterSetupPage: React.FC = () => {
  const apiUrl = "https://localhost:7000/api/characterprofiles";
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
  const [dialoguePairs, setDialoguePairs] = useState<DialoguePair[]>([]);

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

          if (
            data.exampleDialogue &&
            typeof data.exampleDialogue === "string"
          ) {
            try {
              const parsedPairs: { user: string; model: string }[] = JSON.parse(
                data.exampleDialogue
              );

              if (Array.isArray(parsedPairs)) {
                setDialoguePairs(
                  parsedPairs.map((pair, index) => ({
                    // Date.now() を使うと毎回IDが変わる可能性があるので、簡易的なら index でも良いが、
                    // より安定したIDが必要な場合は uuid ライブラリなどを検討
                    id: Date.now() + index, // 簡易的なユニークID生成例
                    user: pair.user ?? "", // null/undefined の場合は空文字に
                    model: pair.model ?? "", // null/undefined の場合は空文字に
                  }))
                );
              } else {
                // パース結果が配列でなかった場合
                console.warn(
                  "Parsed exampleDialogue is not an array:",
                  parsedPairs
                );
                setError("会話例データの形式が不正です (配列ではありません)。");
                setDialoguePairs([]); // 不正な形式なので空にする
              }
            } catch (parseError) {
              // JSON.parse() が失敗した場合
              console.error(
                "Failed to parse exampleDialogue JSON:",
                parseError
              );
              setError(
                "会話例データの読み込みに失敗しました。形式が不正な可能性があります。"
              );
              setDialoguePairs([]); // パース失敗時は空にする
            }
          } else {
            // exampleDialogue が null, undefined, または文字列でない場合
            setDialoguePairs([]); // データがない場合は空配列をセット
          }

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
    } else {
      setName("");
      setPersonality("");
      setTone("");
      setBackstory("");
      setSystemPrompt("");
      setIsActive(true);
      setIsCustomChecked(false);
      setDialoguePairs([]);
      setIsCustomChecked(false);
      setError(null);
    }
  }, [characterId, isEditMode]);

  // --- 会話例ペアの入力値を変更するハンドラ ---
  const handlePairChange = (
    id: number,
    field: "user" | "model",
    value: string
  ) => {
    setDialoguePairs((currentPairs) =>
      currentPairs.map((pair) =>
        // IDが一致するペアを見つけて、指定されたフィールド (user または model) の値を更新
        pair.id === id ? { ...pair, [field]: value } : pair
      )
    );
  };

  // --- 新しい会話例ペアを追加するハンドラ ---
  const handleAddPair = () => {
    // 件数制限 (0～10件なので、10件未満の場合のみ追加)
    if (dialoguePairs.length >= 10) {
      alert("会話例は 10 件まで登録できます。");
      return; // 10件に達していたら追加しない
    }
    // 新しい空のペアオブジェクトを作成 (ID は簡易的に現在時刻を使用)
    const newPair: DialoguePair = {
      id: Date.now(), // 簡易的なユニークID
      user: "",
      model: "",
    };
    // 現在の配列の末尾に新しいペアを追加して state を更新
    setDialoguePairs((currentPairs) => [...currentPairs, newPair]);
  };

  // --- 会話例ペアを削除するハンドラ ---
  const handleDeletePair = (id: number) => {
    // 指定された ID *以外* のペアだけをフィルタリングして新しい配列を作成し、state を更新
    setDialoguePairs((currentPairs) =>
      currentPairs.filter((pair) => pair.id !== id)
    );
  };

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault(); // デフォルトのフォーム送信を抑制
    setIsSubmitting(true); // 送信開始
    setSubmitError(null); // エラーをクリア

    // --- TODO: ここでクライアントサイドバリデーションを追加する (任意) ---
    // 例: if (name.length === 0) { setSubmitError("名前は必須です"); setIsSubmitting(false); return; }

    let dialogueJsonString: string | null = null; // 結果を格納する変数
    try {
      // dialoguePairs 配列から、API に送る形式 ({ user, model } のみ) の配列を作成
      // 各ペアから 'id' プロパティを除外する
      const pairsToSerialize = dialoguePairs.map(({ id, ...rest }) => rest);

      // 配列が空でなければ JSON 文字列化する
      if (pairsToSerialize.length > 0) {
        dialogueJsonString = JSON.stringify(pairsToSerialize);
      }
    } catch (stringifyError) {
      console.error("Failed to stringify dialogue pairs:", stringifyError);
      setError("会話例データの保存形式への変換に失敗しました。");
      setIsSubmitting(false);
      return;
    }

    try {
      let response: Response;

      if (isEditMode && characterId) {
        // --- 更新 (PUT) ---
        const requestData: UpdateCharacterProfileRequest = {
          // UpdateCharacterProfileRequest に対応するオブジェクト
          name,
          personality,
          tone,
          backstory,
          systemPrompt: systemPrompt || null,
          exampleDialogue: dialogueJsonString,
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

  const handleDelete = async () => {
    // 編集モードでない、または characterId が確定していない場合は何もしない
    if (!isEditMode || !characterId) return;

    // 削除確認ダイアログを表示
    // ユーザーが「キャンセル」を選んだら confirm は false を返す
    if (
      window.confirm(
        `キャラクター「${
          name || "未名のキャラクター"
        }」(ID: ${characterId}) を本当に削除しますか？\nこの操作は元に戻せません。`
      )
    ) {
      setIsSubmitting(true); // 処理開始 (ボタンを無効化するために流用)
      setSubmitError(null); // 既存のエラーメッセージをクリア

      try {
        console.log(`Deleting character: ID=${characterId}`); // デバッグログ

        // DELETE リクエストを送信
        const response = await fetch(`${apiUrl}/${characterId}`, {
          method: "DELETE",
        });

        if (!response.ok) {
          const errorData = await response.json().catch(() => ({
            message: `削除リクエストに失敗しました (ステータス: ${response.status})`,
          }));
          throw new Error(
            errorData.message ||
              `削除リクエストに失敗しました (ステータス: ${response.status})`
          );
        }

        // --- 削除成功時の処理 ---
        alert(`キャラクター (ID: ${characterId}) を削除しました。`);
        navigate("/characters"); // キャラクター一覧画面へ遷移
      } catch (err) {
        // fetch 自体のエラー、または上記 throw new Error で捕捉
        const errorMessage =
          err instanceof Error
            ? err.message
            : "不明な削除エラーが発生しました。";
        setSubmitError(`削除エラー: ${errorMessage}`);
        console.error("Delete character error:", err);
        // エラーが発生した場合、ユーザーにメッセージを表示した上で現在の画面に留まる
      } finally {
        // 成功・失敗に関わらず、処理が終わったらボタンを有効に戻す
        setIsSubmitting(false);
      }
    } else {
      console.log("削除がキャンセルされました。");
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
        <div className={styles.formGroup}>
          <label htmlFor="name">
            名前 <span className={styles.required}>*</span>:
          </label>
          <input
            type="text"
            id="name"
            value={name}
            onChange={(e) => setName(e.target.value)}
            required
            maxLength={30} // バリデーション（サーバー側とも合わせる）
            className={styles.input}
          />
        </div>

        <div className={styles.formGroup}>
          <label htmlFor="personality">
            性格 <span className={styles.required}>*</span>:
          </label>
          <textarea
            id="personality"
            value={personality}
            onChange={(e) => setPersonality(e.target.value)}
            required
            rows={3}
            className={styles.textarea}
          />
        </div>

        <div className={styles.formGroup}>
          <label htmlFor="tone">
            口調 <span className={styles.required}>*</span>:
          </label>
          <textarea
            id="tone"
            value={tone}
            onChange={(e) => setTone(e.target.value)}
            required
            rows={3}
            className={styles.textarea}
          />
        </div>

        <div className={styles.formGroup}>
          <label htmlFor="backstory">
            背景 <span className={styles.required}>*</span>:
          </label>
          <textarea
            id="backstory"
            value={backstory}
            onChange={(e) => setBackstory(e.target.value)}
            required
            rows={5}
            className={styles.textarea}
          />
        </div>

        <div className={styles.formGroup}>
          <label htmlFor="systemPrompt" className={styles.label}>
            システムプロンプト ({isCustomChecked ? "カスタム入力" : "自動生成"}
            ):
          </label>
          <div
            className={styles.formGroup}
            style={{
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
            <label htmlFor="isCustomChecked" style={{ cursor: "pointer" }}>
              システムプロンプトをカスタムする
            </label>
          </div>
          <textarea
            id="systemPrompt"
            value={systemPrompt}
            onChange={(e) => setSystemPrompt(e.target.value)}
            rows={5}
            placeholder={
              isCustomChecked ? "カスタムプロンプトを入力" : "自動生成されます"
            }
            className={styles.textarea}
            disabled={!isCustomChecked}
          />
          {!isCustomChecked && (
            <small className={styles.hintText}>
              カスタムチェックを入れると編集できます。
            </small>
          )}
        </div>

        {/* TODO: ExampleDialogue の動的入力 UI をここに実装 */}
        <div className={styles.formGroup}>
          <label className={styles.label}>会話例 (任意、0～10件):</label>
          {/* dialoguePairs 配列をループして各ペアの入力欄とボタンを表示 */}
          {dialoguePairs.map((pair, index) => (
            // 各ペアのコンテナ (key にはユニークな pair.id を指定)
            <div key={pair.id} className={styles.pairContainer}>
              {/* ユーザー発言入力 */}
              <div style={{ marginRight: "1rem", flexGrow: 1 }}>
                <label htmlFor={`user-${pair.id}`} className={styles.subLabel}>
                  ユーザー発言 {index + 1}:
                </label>
                <textarea
                  id={`user-${pair.id}`}
                  value={pair.user}
                  // 入力値が変わったら handlePairChange を呼び出す
                  onChange={(e) =>
                    handlePairChange(pair.id, "user", e.target.value)
                  }
                  rows={2} // 適宜調整
                  className={styles.textarea}
                />
              </div>
              {/* モデル応答入力 */}
              <div style={{ flexGrow: 1 }}>
                <label htmlFor={`model-${pair.id}`} className={styles.subLabel}>
                  モデル応答 {index + 1}:
                </label>
                <textarea
                  id={`model-${pair.id}`}
                  value={pair.model}
                  // 入力値が変わったら handlePairChange を呼び出す
                  onChange={(e) =>
                    handlePairChange(pair.id, "model", e.target.value)
                  }
                  rows={2} // 適宜調整
                  className={styles.textarea}
                />
              </div>
              {/* このペアを削除するボタン */}
              <button
                type="button" // form の submit をトリガーしないように type="button" を指定
                onClick={() => handleDeletePair(pair.id)} // クリックで handleDeletePair を呼び出す
                className={styles.deletePairButton}
                title="この会話例を削除" // マウスオーバーで説明表示
              >
                × {/* バツ印 */}
              </button>
            </div>
          ))}

          {/* 会話例を追加するボタン (10件未満の場合のみ表示) */}
          {dialoguePairs.length < 10 && (
            <button
              type="button"
              onClick={handleAddPair}
              className={styles.addPairButton}
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
            type="url"
            id="avatarImageUrl"
            value={avatarImageUrl}
            onChange={(e) => setAvatarImageUrl(e.target.value)}
            className={styles.input}
          />
        </div>

        <div className={styles.formGroup}>
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
        <div className={styles.buttonGroup}>
          <button
            type="submit"
            className={styles.button}
            disabled={isSubmitting}
          >
            {isEditMode ? "更新" : "登録"}
          </button>
          {/* 編集モードの場合のみ削除ボタンを表示 */}
          {isEditMode && (
            <button
              type="button"
              onClick={handleDelete}
              className={`${styles.button} ${styles.deleteButton}`}
              disabled={isSubmitting}
            >
              削除
            </button>
          )}
          {/* TODO: 保存後に有効化する「会話する」ボタン */}
          {isEditMode && (
            <button
              type="button"
              disabled
              /* onClick={handleStartChat} */
              className={styles.button}
              style={{
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
