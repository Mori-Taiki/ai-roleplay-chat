import { AiGenerationSettingsResponse } from './AiGenerationSettings';

export interface CharacterProfileResponse {
    id: number;
    name: string;
    personality: string | null;
    tone: string | null;
    backstory: string | null;
    systemPrompt: string | null;
    exampleDialogue: string | null; // これは JSON 文字列の想定
    avatarImageUrl: string | null;
    isActive: boolean;
    isSystemPromptCustomized: boolean;
    appearance: string | null;
    userAppellation: string | null;
    aiSettings: AiGenerationSettingsResponse | null;
    // 必要に応じて createdAt や updatedAt も追加
    // createdAt?: string; // APIが文字列で返す場合
    // updatedAt?: string;
  }
  
export interface CharacterProfileWithSessionInfoResponse {
    id: number;
    name: string;
    personality: string | null;
    tone: string | null;
    backstory: string | null;
    systemPrompt: string | null;
    exampleDialogue: string | null;
    avatarImageUrl: string | null;
    isActive: boolean;
    isSystemPromptCustomized: boolean;
    appearance: string | null;
    userAppellation: string | null;
    aiSettings: AiGenerationSettingsResponse | null;
    // セッション情報 (オプショナル)
    sessionId?: string; // Guid?
    lastMessageSnippet?: string; // string?
}
  