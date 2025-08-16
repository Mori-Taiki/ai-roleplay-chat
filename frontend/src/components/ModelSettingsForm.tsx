import React from 'react';
import { UseFormRegister, UseFormWatch } from 'react-hook-form';
import styles from '../pages/SettingsPage.module.css';

interface ModelSettingsFormData {
  geminiChatModel: string;
  geminiImagePromptModel: string;
  replicateImageModel: string;
  geminiImagePromptInstruction: string;
}

interface ModelSettingsFormProps {
  register: UseFormRegister<any>;
  watch?: UseFormWatch<any>;
  showToggle?: boolean;
  showFallbackNote?: boolean;
}

const ModelSettingsForm: React.FC<ModelSettingsFormProps> = ({ 
  register, 
  showToggle = false, 
  showFallbackNote = false 
}) => {
  const [isVisible, setIsVisible] = React.useState(!showToggle);

  if (showToggle && !isVisible) {
    return (
      <div className={styles.section}>
        <button 
          type="button"
          className={styles.toggleButton}
          onClick={() => setIsVisible(true)}
        >
          生成AIを個別に設定する
        </button>
      </div>
    );
  }

  return (
    <section className={styles.section}>
      <div className={styles.sectionHeader}>
        <h2>モデル設定</h2>
        {showToggle && (
          <button 
            type="button"
            className={styles.toggleButton}
            onClick={() => setIsVisible(false)}
          >
            設定を隠す
          </button>
        )}
      </div>
      <p className={styles.description}>
        各サービスで使用するAIモデルのIDやバージョンを指定します。空欄の場合は
        {showFallbackNote ? 'ユーザーのグローバル設定またはシステムのデフォルト値' : 'システムのデフォルト値'}
        が使用されます。
      </p>

      <div className={styles.form}>
        <div className={styles.formField}>
          <label htmlFor="geminiChatModel">Gemini (チャット用モデル)</label>
          <input
            id="geminiChatModel"
            type="text"
            placeholder="例: gemini-2.5-flash"
            {...register('geminiChatModel')}
          />
        </div>

        <div className={styles.formField}>
          <label htmlFor="geminiImagePromptModel">Gemini (画像プロンプト生成用モデル)</label>
          <input
            id="geminiImagePromptModel"
            type="text"
            placeholder="例: gemini-2.5-flash-light"
            {...register('geminiImagePromptModel')}
          />
        </div>

        <div className={styles.formField}>
          <label htmlFor="replicateImageModel">Replicate (画像生成用モデルバージョン)</label>
          <input
            id="replicateImageModel"
            type="text"
            placeholder="例: ac732df83cea7fff18b8472768c88ad041fa750ff7682a21affe81863cbe77e4"
            {...register('replicateImageModel')}
          />
        </div>

        <div className={styles.formField}>
          <label htmlFor="geminiImagePromptInstruction">Gemini (画像プロンプト生成用指示)</label>
          
          {/* Visual flow explanation */}
          <div className={styles.flowExplanation}>
            <h4>🖼️ 画像生成の流れ</h4>
            <div className={styles.flowSteps}>
              <div className={styles.flowStep}>
                <span className={styles.stepNumber}>1</span>
                <span className={styles.stepText}>チャット内容＋キャラクター設定</span>
              </div>
              <div className={styles.flowArrow}>→</div>
              <div className={styles.flowStep}>
                <span className={styles.stepNumber}>2</span>
                <span className={styles.stepText}>Geminiが画像生成用プロンプト(英語）を作成</span>
              </div>
              <div className={styles.flowArrow}>→</div>
              <div className={styles.flowStep}>
                <span className={styles.stepNumber}>3</span>
                <span className={styles.stepText}>Replicateにプロンプトを送信</span>
              </div>
            </div>
            <p className={styles.flowNote}>
              ※ ③で送信されるプロンプトを②でGeminiが生成する際にどのような方針で生成すべきかの指示を入力します。
            </p>
          </div>

          <textarea
            id="geminiImagePromptInstruction"
            rows={15}
            placeholder={`画像プロンプト生成時の指示を入力してください。キャラクター情報は自動で追加されます。

例:
You are an expert in creating high-quality, Danbooru-style prompts for the Animagine XL 3.1 image generation model. Based on the provided Character Profile and conversation history, generate a single, concise English prompt.

## Prompt Generation Rules:
1. **Tag-Based Only:** The entire prompt must be a series of comma-separated tags.
2. **Mandatory Prefixes:** ALWAYS start the prompt with: \`masterpiece, best quality, very aesthetic, absurdres\`.
3. **Rating Modifier:** Immediately after the prefixes, you MUST add ONE of the following rating tags based on the conversation's context.
  - \`safe\`: For wholesome or everyday scenes. (This is the default if unsure).
  - \`sensitive\`: For slightly suggestive content, artistic nudity, swimwear, or mild violence.
  - \`nsfw\`: For explicit themes, non-explicit nudity, or strong violence.
  - \`explicit\`: For pornographic content or extreme violence/gore.

4. **Year Modifier (Optional):** If the context suggests a specific era, you can add ONE of the following: \`newest\`, \`recent\`, \`mid\`, \`early\`, \`oldest\`.

5. **Core Content (Tag Order Matters):** Structure the main part in this order:
  - Subject (e.g., \`1girl\`, \`2boys\`).
  - Character details from the profile (e.g., \`long blonde hair\`, \`blue eyes\`).
  - Scene details from the last message (clothing, pose, emotion, background).

6. **Final Output:** Do not include any explanation or markdown. Only the final comma-separated prompt.

## Example Output:
masterpiece, best quality, very aesthetic, absurdres, safe, newest, 1girl, amelia, from_my_novel, long blonde hair, blue eyes, smiling, wearing a school uniform, sitting on a park bench, sunny day, cherry blossoms

空欄の場合はシステムのデフォルト指示が使用されます。`}
            {...register('geminiImagePromptInstruction')}
          />
          <div className={styles.fieldDescription}>
            <p><strong>📝 指示作成のポイント:</strong></p>
            <ul>
              <li>キャラクター情報（名前、性格、背景、容姿など）は自動で追加されます</li>
              <li>使用する画像生成モデルに応じて指示をカスタマイズしてください</li>
              <li>画像生成には英語のプロンプトが推奨されます(生成用の指示プロンプトそのものは英語でなくても構いません)</li>
              <li>送信されたプロンプトは、画像ギャラリーから各画像の「詳細」ボタンで確認可能です</li>
              <li>各モデルのREADMEや公式ドキュメントを参考にすると良い結果が得られます</li>
              <li>異なるモデル（Stable Diffusion、FLUX等）では異なるプロンプト形式が最適です</li>
            </ul>
          </div>
        </div>
      </div>
    </section>
  );
};

export { ModelSettingsForm };
export type { ModelSettingsFormData };