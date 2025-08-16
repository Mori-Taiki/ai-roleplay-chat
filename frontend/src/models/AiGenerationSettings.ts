export interface AiGenerationSettingsRequest {
  chatGenerationProvider: string | null;
  chatGenerationModel: string | null;
  imagePromptGenerationProvider: string | null;
  imagePromptGenerationModel: string | null;  
  imageGenerationProvider: string | null;
  imageGenerationModel: string | null;
  imageGenerationPromptInstruction: string | null;
}

export interface AiGenerationSettingsResponse {
  id: number;
  settingsType: string;
  chatGenerationProvider: string | null;
  chatGenerationModel: string | null;
  imagePromptGenerationProvider: string | null;
  imagePromptGenerationModel: string | null;
  imageGenerationProvider: string | null;
  imageGenerationModel: string | null;
  imageGenerationPromptInstruction: string | null;
}