export interface ChatSessionResponse {
  id: string;
  characterProfileId: number;
  startTime: string;
  endTime?: string;
  createdAt: string;
  updatedAt: string;
  lastMessageSnippet?: string;
  messageCount: number;
}