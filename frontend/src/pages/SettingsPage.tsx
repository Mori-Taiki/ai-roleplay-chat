import React, { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { useApiKeys } from '../hooks/useApiKeys';
import { useUserSettings, UserSetting } from '../hooks/useUserSettings';
import { useAuth } from '../hooks/useAuth';
import { useNotification } from '../hooks/useNotification';
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
  const { addNotification, removeNotification } = useNotification();
  const { settings, isLoading, error, fetchUserSettings, updateUserSettings } = useUserSettings();

  const {
    registeredServices: registeredApiKeys,
    isLoading: isLoadingApiKeys,
    error: apiKeyError,
    getUserApiKeys,
    registerApiKey,
    deleteApiKey,
  } = useApiKeys();

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
        geminiChatModel:
          settings.find((s) => s.serviceType === 'Gemini' && s.settingKey === 'ChatModel')?.settingValue || '',
        geminiImagePromptModel:
          settings.find((s) => s.serviceType === 'Gemini' && s.settingKey === 'ImagePromptGenerationModel')
            ?.settingValue || '',
        replicateImageModel:
          settings.find((s) => s.serviceType === 'Replicate' && s.settingKey === 'ImageGenerationVersion')
            ?.settingValue || '',
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

  const handleModelSettingsSubmit = async (data: ModelSettingsForm) => {
    const settingsToUpdate: UserSetting[] = [
      { serviceType: 'Gemini', settingKey: 'ChatModel', settingValue: data.geminiChatModel },
      { serviceType: 'Gemini', settingKey: 'ImagePromptGenerationModel', settingValue: data.geminiImagePromptModel },
      { serviceType: 'Replicate', settingKey: 'ImageGenerationVersion', settingValue: data.replicateImageModel },
    ];

    const success = await updateUserSettings(settingsToUpdate);
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
