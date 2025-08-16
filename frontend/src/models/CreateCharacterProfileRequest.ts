import { AiGenerationSettingsRequest } from './AiGenerationSettings';

export interface CreateCharacterProfileRequest {
    name: string;
    personality: string | null;
    tone: string | null;
    backstory: string | null;
    systemPrompt: string | null;
    exampleDialogue: string | null;
    avatarImageUrl: string | null;
    isActive: boolean;
    appearance: string | null;
    userAppellation: string | null;
    aiSettings: AiGenerationSettingsRequest | null;
  }