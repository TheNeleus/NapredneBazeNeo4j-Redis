import apiClient from './axiosClient';
import type { UpdateUserDto } from '../models/User'; 



export const updateUserProfile = async (userId: string, data: UpdateUserDto) => {
  const response = await apiClient.put(`/users/${userId}`, data);
  return response.data;
};


export const addFriendByEmail = async (userId: string, email: string) => {
  const response = await apiClient.post(`/users/${userId}/friend`, { email });
  return response.data;
};  