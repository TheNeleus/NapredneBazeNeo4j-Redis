import apiClient from './axiosClient';
import type { ChatMessage } from '../models/Chat';


export const getChatHistory = async (eventId: string, page: number = 0): Promise<ChatMessage[]> => {
  const response = await apiClient.get<ChatMessage[]>(`/chat/${eventId}/history?page=${page}`);
  return response.data;
};