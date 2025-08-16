export interface UpdateCharacterProfileRequest {
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
    textModelProvider: string | null;
    textModelId: string | null;
    imageModelProvider: string | null;
    imageModelId: string | null;
  }