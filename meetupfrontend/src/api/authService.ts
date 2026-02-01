import apiClient from './axiosClient';
import type { LoginResponse, User } from '../models/User';

export const register = async (userData: User): Promise<User> => {
  const response = await apiClient.post<User>('/users', userData);
  return response.data;
};

export const login = async (email: string): Promise<LoginResponse> => {
  // Backend ocekuje JSON string, npr: "filip@email.com" (sa navodnicima)
  // Zato koristimo JSON.stringify da pretvorimo obican string u JSON string.
  const response = await apiClient.post<LoginResponse>('/users/login', JSON.stringify(email));
  return response.data;
};

export const logout = () => {
  localStorage.removeItem('meetup_token');
  localStorage.removeItem('meetup_user');
  window.location.href = '/'; // Vrati ga na login
};