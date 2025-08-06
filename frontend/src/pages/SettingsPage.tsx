import React, { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { useApiKeys } from '../hooks/useApiKeys';
import { useAuth } from '../hooks/useAuth';
import styles from './SettingsPage.module.css';

interface KeyVaultUriForm {
  keyVaultUri: string;
}

interface ApiKeyForm {
  serviceName: string;
  apiKey: string;
}

const SUPPORTED_SERVICES = ['Gemini', 'Replicate'] as const;

const SettingsPage: React.FC = () => {
  const { isAuthenticated } = useAuth();
  const {
    registeredServices,
    keyVaultUri,
    isLoading,
    error,
    getUserApiKeys,
    registerApiKey,
    deleteApiKey,
    setKeyVaultUri,
    clearError,
  } = useApiKeys();

  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  // Key Vault URI form
  const keyVaultForm = useForm<KeyVaultUriForm>({
    defaultValues: {
      keyVaultUri: '',
    },
  });

  // API Key form
  const apiKeyForm = useForm<ApiKeyForm>({
    defaultValues: {
      serviceName: 'Gemini',
      apiKey: '',
    },
  });

  // Load user's API keys on mount
  useEffect(() => {
    if (isAuthenticated) {
      getUserApiKeys();
    }
  }, [isAuthenticated, getUserApiKeys]);

  // Update Key Vault URI form when data is loaded
  useEffect(() => {
    if (keyVaultUri) {
      keyVaultForm.setValue('keyVaultUri', keyVaultUri);
    }
  }, [keyVaultUri, keyVaultForm]);

  const clearMessages = () => {
    setSuccessMessage(null);
    clearError();
  };

  const handleKeyVaultUriSubmit = async (data: KeyVaultUriForm) => {
    clearMessages();
    
    const success = await setKeyVaultUri(data.keyVaultUri);
    if (success) {
      setSuccessMessage('Key Vault URIが正常に設定されました。');
    }
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
      
      {successMessage && (
        <div className={styles.successMessage}>
          {successMessage}
        </div>
      )}

      {/* Loading Indicator */}
      {isLoading && (
        <div className={styles.loading}>
          読み込み中...
        </div>
      )}

      {/* Key Vault URI Section */}
      <section className={styles.section}>
        <h2>Key Vault URI</h2>
        <p className={styles.description}>
          Azure Key Vaultのエンドポイントを設定します。設定後、APIキーはこのKey Vaultに安全に保存されます。
        </p>
        
        <form onSubmit={keyVaultForm.handleSubmit(handleKeyVaultUriSubmit)} className={styles.form}>
          <div className={styles.formField}>
            <label htmlFor="keyVaultUri">Key Vault URI</label>
            <input
              id="keyVaultUri"
              type="url"
              placeholder="https://your-keyvault.vault.azure.net/"
              {...keyVaultForm.register('keyVaultUri', {
                required: 'Key Vault URIを入力してください',
                pattern: {
                  value: /^https:\/\/.*\.vault\.azure\.net\/?$/,
                  message: '有効なAzure Key Vault URIを入力してください（例：https://your-keyvault.vault.azure.net/）'
                }
              })}
              className={keyVaultForm.formState.errors.keyVaultUri ? styles.errorInput : ''}
            />
            {keyVaultForm.formState.errors.keyVaultUri && (
              <span className={styles.fieldError}>
                {keyVaultForm.formState.errors.keyVaultUri.message}
              </span>
            )}
          </div>
          
          <button type="submit" disabled={isLoading} className={styles.primaryButton}>
            Key Vault URIを設定
          </button>
        </form>
      </section>

      {/* Current Status Section */}
      <section className={styles.section}>
        <h2>現在の設定状況</h2>
        
        <div className={styles.statusGrid}>
          <div className={styles.statusItem}>
            <strong>Key Vault URI:</strong>
            <span className={keyVaultUri ? styles.statusActive : styles.statusInactive}>
              {keyVaultUri || '未設定'}
            </span>
          </div>
          
          {SUPPORTED_SERVICES.map((service) => (
            <div key={service} className={styles.statusItem}>
              <strong>{service} API Key:</strong>
              <span className={registeredServices.includes(service) ? styles.statusActive : styles.statusInactive}>
                {registeredServices.includes(service) ? '登録済み' : '未登録'}
              </span>
              {registeredServices.includes(service) && (
                <button
                  onClick={() => handleDeleteApiKey(service)}
                  disabled={isLoading}
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
          外部サービスのAPIキーを登録します。キーは設定したKey Vaultに安全に保存されます。
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
          
          <button type="submit" disabled={isLoading} className={styles.primaryButton}>
            APIキーを登録
          </button>
        </form>
      </section>
    </div>
  );
};

export default SettingsPage;