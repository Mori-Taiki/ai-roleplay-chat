export interface AiGenerationSettingsRequest {
  chatGenerationModel: string | null;
  imagePromptGenerationModel: string | null;  
  imageGenerationModel: string | null;
  imageGenerationPromptInstruction: string | null;
}

export interface AiGenerationSettingsResponse {
  id: number;
  chatGenerationModel: string | null;
  imagePromptGenerationModel: string | null;
  imageGenerationModel: string | null;
  imageGenerationPromptInstruction: string | null;
}