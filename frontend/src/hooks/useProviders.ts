// Hook for managing available AI providers and models
import { useState } from 'react';

export interface TextModelOption {
  modelId: string;
  provider: string;
  displayName: string;
  maxTokens?: number;
  temperature?: number;
  isEnabled: boolean;
}

export interface ImageModelOption {
  modelId: string;
  provider: string;
  displayName: string;
  width?: number;
  height?: number;
  isEnabled: boolean;
}

export interface ProviderOptions {
  default: {
    textProvider: string;
    textModel: string;
    imageProvider: string;
    imageModel: string;
  };
  textModels: Record<string, TextModelOption>;
  imageModels: Record<string, ImageModelOption>;
}

// Hardcoded provider options based on the current appsettings.json
// In the future, this could be fetched from an API endpoint
const defaultProviderOptions: ProviderOptions = {
  default: {
    textProvider: "Gemini",
    textModel: "gemini-1.5-flash-latest",
    imageProvider: "Replicate",
    imageModel: "0fc0fa9885b284901a6f9c0b4d67701fd7647d157b88371427d63f8089ce140e"
  },
  textModels: {
    "gemini-1.5-flash-latest": {
      modelId: "gemini-1.5-flash-latest",
      provider: "Gemini",
      displayName: "Gemini 1.5 Flash",
      maxTokens: 1024,
      temperature: 0.7,
      isEnabled: true
    },
    "gemini-2.5-pro-preview-05-06": {
      modelId: "gemini-2.5-pro-preview-05-06",
      provider: "Gemini",
      displayName: "Gemini 2.5 Pro Preview",
      maxTokens: 2048,
      temperature: 0.7,
      isEnabled: true
    }
  },
  imageModels: {
    "replicate-flux-dev": {
      modelId: "0fc0fa9885b284901a6f9c0b4d67701fd7647d157b88371427d63f8089ce140e",
      provider: "Replicate",
      displayName: "FLUX.1 [dev]",
      width: 1024,
      height: 1024,
      isEnabled: true
    }
  }
};

export const useProviders = () => {
  const [providers] = useState<ProviderOptions>(defaultProviderOptions);
  const [isLoading] = useState(false);
  const [error] = useState<string | null>(null);

  // Get available text providers
  const getTextProviders = () => {
    const providerSet = new Set<string>();
    Object.values(providers.textModels)
      .filter(model => model.isEnabled)
      .forEach(model => providerSet.add(model.provider));
    return Array.from(providerSet);
  };

  // Get text models for a specific provider
  const getTextModelsForProvider = (provider: string) => {
    return Object.entries(providers.textModels)
      .filter(([, model]) => model.isEnabled && model.provider === provider)
      .map(([key, model]) => ({
        value: model.modelId,
        label: model.displayName,
        key
      }));
  };

  // Get available image providers
  const getImageProviders = () => {
    const providerSet = new Set<string>();
    Object.values(providers.imageModels)
      .filter(model => model.isEnabled)
      .forEach(model => providerSet.add(model.provider));
    return Array.from(providerSet);
  };

  // Get image models for a specific provider
  const getImageModelsForProvider = (provider: string) => {
    return Object.entries(providers.imageModels)
      .filter(([, model]) => model.isEnabled && model.provider === provider)
      .map(([key, model]) => ({
        value: model.modelId,
        label: model.displayName,
        key
      }));
  };

  return {
    providers,
    isLoading,
    error,
    getTextProviders,
    getTextModelsForProvider,
    getImageProviders,
    getImageModelsForProvider,
  };
};