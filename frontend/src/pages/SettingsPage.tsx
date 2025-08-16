import React, { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { useApiKeys } from '../hooks/useApiKeys';
import { useUserAiSettings } from '../hooks/useUserAiSettings';
import { useAuth } from '../hooks/useAuth';
import { useNotification } from '../hooks/useNotification';
import { ModelSettingsForm, ModelSettingsFormData } from '../components/ModelSettingsForm';
import { AiGenerationSettingsRequest } from '../models/AiGenerationSettings';
import styles from './SettingsPage.module.css';

interface ApiKeyForm {
  serviceName: string;
  apiKey: string;
}

const SUPPORTED_SERVICES = ['Gemini', 'Replicate'] as const;

const SettingsPage: React.FC = () => {
  const { isAuthenticated } = useAuth();
  const { addNotification, removeNotification } = useNotification();
  const { settings, isLoading, error, fetchUserAiSettings, updateUserAiSettings } = useUserAiSettings();

  const {
    registeredServices: registeredApiKeys,
    isLoading: isLoadingApiKeys,
    error: apiKeyError,
    getUserApiKeys,
    registerApiKey,
    deleteApiKey,
  } = useApiKeys();

  const modelSettingsForm = useForm<ModelSettingsFormData>();
  const apiKeyForm = useForm<ApiKeyForm>({
    defaultValues: {
      serviceName: 'Gemini',
      apiKey: '',
    },
  });

  useEffect(() => {
    if (isAuthenticated) {
      fetchUserAiSettings();
      getUserApiKeys();
    }
  }, [isAuthenticated, fetchUserAiSettings, getUserApiKeys]);

  useEffect(() => {
    if (settings) {
      modelSettingsForm.reset({
        geminiChatModel: settings.chatGenerationModel || '',
        geminiImagePromptModel: settings.imagePromptGenerationModel || '',
        replicateImageModel: settings.imageGenerationModel || '',
        geminiImagePromptInstruction: settings.imageGenerationPromptInstruction || '',
      });
    }
  }, [settings, modelSettingsForm.reset]);

  useEffect(() => {
    let loadingNotificationId: string | null = null;
    if (isLoading || isLoadingApiKeys) {
      loadingNotificationId = addNotification({ message: '読み込み中...', type: 'loading' });
    } else {
      if (loadingNotificationId) {
        removeNotification(loadingNotificationId);
      }
    }
    return () => {
      if (loadingNotificationId) {
        removeNotification(loadingNotificationId);
      }
    };
  }, [isLoading, isLoadingApiKeys, addNotification, removeNotification]);

  useEffect(() => {
    if (error) {
      addNotification({ message: error, type: 'error' });
    }
  }, [error, addNotification]);

  useEffect(() => {
    if (apiKeyError) {
      addNotification({ message: apiKeyError, type: 'error' });
    }
  }, [apiKeyError, addNotification]);

  const handleApiKeySubmit = async (data: ApiKeyForm) => {
    const success = await registerApiKey(data.serviceName, data.apiKey);
    if (success) {
      addNotification({ message: `${data.serviceName}のAPIキーが正常に登録されました。`, type: 'success' });
      apiKeyForm.reset({ serviceName: data.serviceName, apiKey: '' });
    }
  };

  const handleDeleteApiKey = async (serviceName: string) => {
    if (!window.confirm(`${serviceName}のAPIキーを削除しますか？`)) {
      return;
    }
    const success = await deleteApiKey(serviceName);
    if (success) {
      addNotification({ message: `${serviceName}のAPIキーが正常に削除されました。`, type: 'success' });
    }
  };

  const handleModelSettingsSubmit = async (data: ModelSettingsFormData) => {
    const settingsToUpdate: AiGenerationSettingsRequest = {
      chatGenerationProvider: data.geminiChatModel ? 'Gemini' : null,
      chatGenerationModel: data.geminiChatModel || null,
      imagePromptGenerationProvider: data.geminiImagePromptModel ? 'Gemini' : null,
      imagePromptGenerationModel: data.geminiImagePromptModel || null,
      imageGenerationProvider: data.replicateImageModel ? 'Replicate' : null,
      imageGenerationModel: data.replicateImageModel || null,
      imageGenerationPromptInstruction: data.geminiImagePromptInstruction || null,
    };

    const success = await updateUserAiSettings(settingsToUpdate);
    if (success) {
      addNotification({ message: 'モデル設定が正常に更新されました。', type: 'success' });
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

      <section className={styles.section}>
        <h2>APIキーの現在の設定状況</h2>
        <div className={styles.statusGrid}>
          {SUPPORTED_SERVICES.map((service) => (
            <div key={service}>
              <div className={styles.statusItem}>
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
            </div>
          ))}
        </div>
        <div className={styles.apiKeyLink}>
          <a href="https://aistudio.google.com/apikey" target="_blank" rel="noopener noreferrer">
            GeminiのAPIキー取得方法はこちら
          </a>
        </div>
        <div className={styles.apiKeyLink}>
          <a href="https://replicate.com/" target="_blank" rel="noopener noreferrer">
            ReplicateのAPIキー取得方法はこちら
          </a>
        </div>
      </section>

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
                  message: 'APIキーは最低10文字以上である必要があります',
                },
              })}
              className={apiKeyForm.formState.errors.apiKey ? styles.errorInput : ''}
            />
            {apiKeyForm.formState.errors.apiKey && (
              <span className={styles.fieldError}>{apiKeyForm.formState.errors.apiKey.message}</span>
            )}
          </div>

          <button type="submit" disabled={isLoadingApiKeys} className={styles.primaryButton}>
            APIキーを登録
          </button>
        </form>
      </section>

      <form onSubmit={modelSettingsForm.handleSubmit(handleModelSettingsSubmit)}>
        <ModelSettingsForm 
          register={modelSettingsForm.register}
          watch={modelSettingsForm.watch}
        />
        <div className={styles.section}>
          <button type="submit" disabled={isLoading} className={styles.primaryButton}>
            モデル設定を保存
          </button>
        </div>
      </form>
    </div>
  );
};

export default SettingsPage;