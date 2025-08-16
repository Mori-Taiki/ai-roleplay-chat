import React from 'react';
import { AiGenerationSettingsRequest } from '../models/AiGenerationSettings';
import styles from '../pages/CharacterSetupPage.module.css';

interface SimpleFormFieldProps {
  label: string;
  name: string;
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
  type?: 'text' | 'textarea' | 'select';
  rows?: number;
  options?: { value: string; label: string }[];
}

const SimpleFormField: React.FC<SimpleFormFieldProps> = ({ 
  label, 
  name, 
  value, 
  onChange, 
  placeholder,
  type = 'text',
  rows = 3,
  options = []
}) => {
  return (
    <div className={styles.formField}>
      <label htmlFor={name} className={styles.label}>
        {label}
      </label>
      {type === 'textarea' ? (
        <textarea
          id={name}
          name={name}
          value={value}
          onChange={(e) => onChange(e.target.value)}
          placeholder={placeholder}
          rows={rows}
          className={styles.textarea}
        />
      ) : type === 'select' ? (
        <select
          id={name}
          name={name}
          value={value}
          onChange={(e) => onChange(e.target.value)}
          className={styles.select}
        >
          <option value="">選択してください</option>
          {options.map((option) => (
            <option key={option.value} value={option.value}>
              {option.label}
            </option>
          ))}
        </select>
      ) : (
        <input
          id={name}
          name={name}
          type="text"
          value={value}
          onChange={(e) => onChange(e.target.value)}
          placeholder={placeholder}
          className={styles.input}
        />
      )}
    </div>
  );
};

interface AiModelSettingsFormProps {
  aiSettings: AiGenerationSettingsRequest | null;
  onSettingsChange: (settings: AiGenerationSettingsRequest | null) => void;
  showFallbackNote?: boolean;
  showToggleButton?: boolean;
}

const AiModelSettingsForm: React.FC<AiModelSettingsFormProps> = ({ 
  aiSettings, 
  onSettingsChange, 
  showFallbackNote = true,
  showToggleButton = false
}) => {
  const [isVisible, setIsVisible] = React.useState(!showToggleButton);

  const handleFieldChange = (field: keyof AiGenerationSettingsRequest, value: string) => {
    const newSettings = {
      chatGenerationProvider: aiSettings?.chatGenerationProvider || null,
      chatGenerationModel: aiSettings?.chatGenerationModel || null,
      imagePromptGenerationProvider: aiSettings?.imagePromptGenerationProvider || null,
      imagePromptGenerationModel: aiSettings?.imagePromptGenerationModel || null,
      imageGenerationProvider: aiSettings?.imageGenerationProvider || null,
      imageGenerationModel: aiSettings?.imageGenerationModel || null,
      imageGenerationPromptInstruction: aiSettings?.imageGenerationPromptInstruction || null,
      ...{ [field]: value || null }
    };
    
    // Check if all fields are empty, if so, set to null
    const hasAnyValue = Object.values(newSettings).some(val => val !== null && val !== '');
    onSettingsChange(hasAnyValue ? newSettings : null);
  };

  const providerOptions = [
    { value: 'Gemini', label: 'Gemini' },
    { value: 'Replicate', label: 'Replicate' }
  ];

  if (showToggleButton && !isVisible) {
    return (
      <div className={styles.modelSection}>
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
    <div className={styles.modelSection}>
      <div className={styles.sectionHeader}>
        <h3>AIモデル設定</h3>
        {showToggleButton && (
          <button 
            type="button"
            className={styles.toggleButton}
            onClick={() => setIsVisible(false)}
          >
            設定を隠す
          </button>
        )}
      </div>
      {showFallbackNote && (
        <p className={styles.fallbackNote}>
          未設定の場合は、ユーザーのグローバル設定またはシステムデフォルトが使用されます
        </p>
      )}
      
      <div className={styles.modelFieldGroup}>
        <h4 className={styles.fieldGroupTitle}>チャット生成</h4>
        <div className={styles.providerModelRow}>
          <SimpleFormField
            label="プロバイダー"
            name="chatGenerationProvider"
            type="select"
            value={aiSettings?.chatGenerationProvider || ''}
            onChange={(value) => handleFieldChange('chatGenerationProvider', value)}
            options={providerOptions}
          />
          <SimpleFormField
            label="モデル"
            name="chatGenerationModel"
            value={aiSettings?.chatGenerationModel || ''}
            onChange={(value) => handleFieldChange('chatGenerationModel', value)}
            placeholder="例: gemini-1.5-flash-latest"
          />
        </div>
      </div>

      <div className={styles.modelFieldGroup}>
        <h4 className={styles.fieldGroupTitle}>画像プロンプト生成</h4>
        <div className={styles.providerModelRow}>
          <SimpleFormField
            label="プロバイダー"
            name="imagePromptGenerationProvider"
            type="select"
            value={aiSettings?.imagePromptGenerationProvider || ''}
            onChange={(value) => handleFieldChange('imagePromptGenerationProvider', value)}
            options={providerOptions}
          />
          <SimpleFormField
            label="モデル"
            name="imagePromptGenerationModel"
            value={aiSettings?.imagePromptGenerationModel || ''}
            onChange={(value) => handleFieldChange('imagePromptGenerationModel', value)}
            placeholder="例: gemini-1.5-flash-latest"
          />
        </div>
      </div>

      <div className={styles.modelFieldGroup}>
        <h4 className={styles.fieldGroupTitle}>画像生成</h4>
        <div className={styles.providerModelRow}>
          <SimpleFormField
            label="プロバイダー"
            name="imageGenerationProvider"
            type="select"
            value={aiSettings?.imageGenerationProvider || ''}
            onChange={(value) => handleFieldChange('imageGenerationProvider', value)}
            options={providerOptions}
          />
          <SimpleFormField
            label="モデル"
            name="imageGenerationModel"
            value={aiSettings?.imageGenerationModel || ''}
            onChange={(value) => handleFieldChange('imageGenerationModel', value)}
            placeholder="例: black-forest-labs/flux-1-dev"
          />
        </div>
      </div>

      <div className={styles.modelField}>
        <SimpleFormField
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