import apiClient from './axiosClient';

export interface UserProfileUpdate {
    id: string;
    name: string;
    email: string;
    bio?: string;
    interests?: string[];
}

export const updateUserProfile = async (userId: string, data: UserProfileUpdate) => {
  const response = await apiClient.put(`/users/${userId}`, data);
  return response.data;
};


export const addFriendByEmail = async (userId: string, email: string) => {
  const response = await apiClient.post(`/users/${userId}/friend`, { email });
  return response.data;
};  