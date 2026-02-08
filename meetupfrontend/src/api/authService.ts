import apiClient from './axiosClient';
import type { LoginResponse, User } from '../models/User';



export const register = async (userData: User): Promise<User> => {
  const response = await apiClient.post<User>('/users', userData);
  return response.data;
};

export const login = async (email: string): Promise<LoginResponse> => {
  const response = await apiClient.post<LoginResponse>('/users/login', { email });
  
  if (response.data.token) {
      sessionStorage.setItem('meetup_token', response.data.token);
      sessionStorage.setItem('meetup_user', JSON.stringify(response.data.user)); 
  }
  
  return response.data;
};

export const logout = async () => {
  try {
      await apiClient.post('/users/logout');
  } catch (error) {
      console.error("Logout failed on server", error);
  } finally {
      sessionStorage.removeItem('meetup_token');
      sessionStorage.removeItem('meetup_user');
      window.location.href = '/'; 
  }
};