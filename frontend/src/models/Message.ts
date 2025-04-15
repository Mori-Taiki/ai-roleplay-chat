export interface Message {
  id: string;
  sender: 'user' | 'ai';
  text: string;
  imageUrl?: string;
}