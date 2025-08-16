import React from 'react';
import FormField from './FormField';
import { AiGenerationSettingsRequest } from '../models/AiGenerationSettings';
import styles from '../pages/CharacterSetupPage.module.css';

interface AiModelSettingsFormProps {
  aiSettings: AiGenerationSettingsRequest | null;
  onSettingsChange: (settings: AiGenerationSettingsRequest | null) => void;
  showFallbackNote?: boolean;
}

const AiModelSettingsForm: React.FC<AiModelSettingsFormProps> = ({ 
  aiSettings, 
  onSettingsChange, 
  showFallbackNote = true 
}) => {
  const handleFieldChange = (field: keyof AiGenerationSettingsRequest, value: string) => {
    const newSettings = {
      chatGenerationModel: aiSettings?.chatGenerationModel || null,
      imagePromptGenerationModel: aiSettings?.imagePromptGenerationModel || null,
      imageGenerationModel: aiSettings?.imageGenerationModel || null,
      imageGenerationPromptInstruction: aiSettings?.imageGenerationPromptInstruction || null,
      ...{ [field]: value || null }
    };
    
    // Check if all fields are empty, if so, set to null
    const hasAnyValue = Object.values(newSettings).some(val => val !== null && val !== '');
    onSettingsChange(hasAnyValue ? newSettings : null);
  };

  return (
    <div className={styles.modelSection}>
      <h3>AIモデル設定</h3>
      {showFallbackNote && (
        <p className={styles.fallbackNote}>
          未設定の場合は、ユーザーのグローバル設定またはシステムデフォルトが使用されます
        </p>
      )}
      
      <div className={styles.modelField}>
        <FormField
          label="チャット生成モデル"
          name="chatGenerationModel"
          value={aiSettings?.chatGenerationModel || ''}
          onChange={(value) => handleFieldChange('chatGenerationModel', value)}
          placeholder="例: gemini-1.5-flash-latest"
        />
      </div>

      <div className={styles.modelField}>
        <FormField
          label="画像プロンプト生成モデル"
          name="imagePromptGenerationModel"
          value={aiSettings?.imagePromptGenerationModel || ''}
          onChange={(value) => handleFieldChange('imagePromptGenerationModel', value)}
          placeholder="例: gemini-1.5-flash-latest"
        />
      </div>

      <div className={styles.modelField}>
        <FormField
          label="画像生成モデル"
          name="imageGenerationModel"
          value={aiSettings?.imageGenerationModel || ''}
          onChange={(value) => handleFieldChange('imageGenerationModel', value)}
          placeholder="例: black-forest-labs/flux-1-dev"
        />
      </div>

      <div className={styles.modelField}>
        <FormField
          label="画像生成プロンプト指示"
          name="imageGenerationPromptInstruction"
          type="textarea"
          value={aiSettings?.imageGenerationPromptInstruction || ''}
          onChange={(value) => handleFieldChange('imageGenerationPromptInstruction', value)}
          placeholder="画像生成時に使用するカスタム指示を入力してください..."
          rows={3}
        />
      </div>
    </div>
  );
};

export default AiModelSettingsForm;