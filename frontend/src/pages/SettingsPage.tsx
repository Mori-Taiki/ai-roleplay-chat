import React, { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { useApiKeys } from '../hooks/useApiKeys';
import { useUserSettings, UserSetting } from '../hooks/useUserSettings';
import { useAuth } from '../hooks/useAuth';
import styles from './SettingsPage.module.css';

interface ModelSettingsForm {
  geminiChatModel: string;
  geminiImagePromptModel: string;
  replicateImageModel: string;
}

interface ApiKeyForm {
  serviceName: string;
  apiKey: string;
}

const SUPPORTED_SERVICES = ['Gemini', 'Replicate'] as const;

const SettingsPage: React.FC = () => {
  const { isAuthenticated } = useAuth();
  const {
    settings,
    isLoading,
    error,
    fetchUserSettings,
    updateUserSettings,
    clearError,
  } = useUserSettings();

  const { 
    registeredServices: registeredApiKeys,
    isLoading: isLoadingApiKeys,
    error: apiKeyError,
    getUserApiKeys,
    registerApiKey,
    deleteApiKey,
    clearError: clearApiKeyError,
  } = useApiKeys();

  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  // API Key form
  const modelSettingsForm = useForm<ModelSettingsForm>();

  const apiKeyForm = useForm<ApiKeyForm>({
    defaultValues: {
      serviceName: 'Gemini',
      apiKey: '',
    },
  });

  useEffect(() => {
    if (isAuthenticated) {
      fetchUserSettings();
      getUserApiKeys();
    }
  }, [isAuthenticated, fetchUserSettings, getUserApiKeys]);

  useEffect(() => {
    if (settings) {
      modelSettingsForm.reset({
        geminiChatModel: settings.find(s => s.serviceType === 'Gemini' && s.settingKey === 'ChatModel')?.settingValue || '',
        geminiImagePromptModel: settings.find(s => s.serviceType === 'Gemini' && s.settingKey === 'ImagePromptGenerationModel')?.settingValue || '',
        replicateImageModel: settings.find(s => s.serviceType === 'Replicate' && s.settingKey === 'ImageGenerationVersion')?.settingValue || '',
      });
    }
  }, [settings, modelSettingsForm.reset, modelSettingsForm]);

  const clearMessages = () => {
    setSuccessMessage(null);
    clearError(); // モデル設定のエラーをクリア
    clearApiKeyError(); // APIキーのエラーをクリア
  };

  const handleApiKeySubmit = async (data: ApiKeyForm) => {
    clearMessages();
    
    const success = await registerApiKey(data.serviceName, data.apiKey);
    if (success) {
      setSuccessMessage(`${data.serviceName}のAPIキーが正常に登録されました。`);
      apiKeyForm.reset({ serviceName: data.serviceName, apiKey: '' });
    }
  };

  const handleDeleteApiKey = async (serviceName: string) => {
    if (!window.confirm(`${serviceName}のAPIキーを削除しますか？`)) {
      return;
    }

    clearMessages();
    
    const success = await deleteApiKey(serviceName);
    if (success) {
      setSuccessMessage(`${serviceName}のAPIキーが正常に削除されました。`);
    }
  };

  const handleModelSettingsSubmit = async (data: ModelSettingsForm) => {
    clearMessages();

    const settingsToUpdate: UserSetting[] = [
      { serviceType: 'Gemini', settingKey: 'ChatModel', settingValue: data.geminiChatModel },
      { serviceType: 'Gemini', settingKey: 'ImagePromptGenerationModel', settingValue: data.geminiImagePromptModel },
      { serviceType: 'Replicate', settingKey: 'ImageGenerationVersion', settingValue: data.replicateImageModel },
    ];

    const success = await updateUserSettings(settingsToUpdate);
    if (success) {
      setSuccessMessage('モデル設定が正常に更新されました。');
    }
  };

  if (!isAuthenticated) {
    return (
      <div className={styles.container}>
        <h1>設定</h1>
        <p>設定ページにアクセスするにはログインしてください。</p>
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <h1>設定</h1>
      
      {/* Error and Success Messages */}
      {error && (
        <div className={styles.errorMessage}>
          {error}
        </div>
      )}
      
      {apiKeyError && (
        <div className={styles.errorMessage}>
          {apiKeyError}
        </div>
      )}
      
      {successMessage && (
        <div className={styles.successMessage}>
          {successMessage}
        </div>
      )}

      {/* Loading Indicator */}
      {(isLoading || isLoadingApiKeys) && (
        <div className={styles.loading}>
          読み込み中...
        </div>
      )}


      {/* Current Status Section */}
      <section className={styles.section}>
        <h2>APIキーの現在の設定状況</h2>
        
        <div className={styles.statusGrid}>
          {SUPPORTED_SERVICES.map((service) => (
            <div key={service} className={styles.statusItem}>
              <strong>{service} API Key:</strong>
              <span className={registeredApiKeys.includes(service) ? styles.statusActive : styles.statusInactive}>
                {registeredApiKeys.includes(service) ? '登録済み' : '未登録'}
              </span>
              {registeredApiKeys.includes(service) && (
                <button
                  onClick={() => handleDeleteApiKey(service)}
                  disabled={isLoadingApiKeys}
                  className={styles.dangerButton}
                >
                  削除
                </button>
              )}
            </div>
          ))}
        </div>
      </section>

      {/* API Key Registration Section */}
      <section className={styles.section}>
        <h2>APIキーの登録</h2>
        <p className={styles.description}>
          外部サービスのAPIキーを登録します。キーはシステムで設定されたKey Vaultに安全に保存されます。
        </p>
        
        <form onSubmit={apiKeyForm.handleSubmit(handleApiKeySubmit)} className={styles.form}>
          <div className={styles.formField}>
            <label htmlFor="serviceName">サービス</label>
            <select
              id="serviceName"
              {...apiKeyForm.register('serviceName', { required: 'サービスを選択してください' })}
            >
              {SUPPORTED_SERVICES.map((service) => (
                <option key={service} value={service}>
                  {service}
                </option>
              ))}
            </select>
          </div>

          <div className={styles.formField}>
            <label htmlFor="apiKey">APIキー</label>
            <input
              id="apiKey"
              type="password"
              placeholder="APIキーを入力してください"
              {...apiKeyForm.register('apiKey', {
                required: 'APIキーを入力してください',
                minLength: {
                  value: 10,
                  message: 'APIキーは最低10文字以上である必要があります'
                }
              })}
              className={apiKeyForm.formState.errors.apiKey ? styles.errorInput : ''}
            />
            {apiKeyForm.formState.errors.apiKey && (
              <span className={styles.fieldError}>
                {apiKeyForm.formState.errors.apiKey.message}
              </span>
            )}
          </div>
          
          <button type="submit" disabled={isLoadingApiKeys} className={styles.primaryButton}>
            APIキーを登録
          </button>
        </form>
      </section>

      {/* Model Settings Section */}
      <section className={styles.section}>
        <h2>モデル設定</h2>
        <p className={styles.description}>
          各サービスで使用するAIモデルのIDやバージョンを指定します。空欄の場合はシステムのデフォルト値が使用されます。
        </p>
        
        <form onSubmit={modelSettingsForm.handleSubmit(handleModelSettingsSubmit)} className={styles.form}>
          <div className={styles.formField}>
            <label htmlFor="geminiChatModel">Gemini (チャット用モデル)</label>
            <input
              id="geminiChatModel"
              type="text"
              placeholder="例: gemini-2.5-flash"
              {...modelSettingsForm.register('geminiChatModel')}
            />
          </div>

          <div className={styles.formField}>
            <label htmlFor="geminiImagePromptModel">Gemini (画像プロンプト生成用モデル)</label>
            <input
              id="geminiImagePromptModel"
              type="text"
              placeholder="例: gemini-2.5-flash-light"
              {...modelSettingsForm.register('geminiImagePromptModel')}
            />
          </div>

          <div className={styles.formField}>
            <label htmlFor="replicateImageModel">Replicate (画像生成用モデルバージョン)</label>
            <input
              id="replicateImageModel"
              type="text"
              placeholder="例: ac732df83cea7fff18b8472768c88ad041fa750ff7682a21affe81863cbe77e4"
              {...modelSettingsForm.register('replicateImageModel')}
            />
          </div>
          
          <button type="submit" disabled={isLoading} className={styles.primaryButton}>
            モデル設定を保存
          </button>
        </form>
      </section>
    </div>
  );
};

export default SettingsPage;