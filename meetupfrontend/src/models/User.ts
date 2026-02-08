export interface User {
  id: string;
  name: string;
  email: string;
  interests: string[];
  role: string;
  bio: string;
}

export interface LoginResponse {
  token: string;
  user: User;
}

export interface UpdateUserDto {
  name?: string;
  email?: string;
  interests?: string[];
  bio?: string;
}