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
    textModelProvider: string | null;
    textModelId: string | null;
    imageModelProvider: string | null;
    imageModelId: string | null;
    // 必要に応じて createdAt や updatedAt も追加
    // createdAt?: string; // APIが文字列で返す場合
    // updatedAt?: string;
  }
  export interface CharacterProfileWithSessionInfoResponse {
    id: string; // Guid は string で扱う
    name: string;
    avatarImageUrl?: string;
    // 必要に応じて CharacterProfile の他の基本情報プロパティを追加
    // personality?: string;
    // tone?: string;
    // backstory?: string;
    // isActive?: boolean;
    // isSystemPromptCustomized?: boolean;
  
    // セッション情報 (オプショナル)
    sessionId?: string; // Guid?
    lastMessageSnippet?: string; // string?
    appearance?: string | null;
    userAppellation?: string | null;
  }
  