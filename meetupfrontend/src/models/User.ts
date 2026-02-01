export interface User {
  id: string;
  name: string;
  email: string;
  interests: string[];
  role: string;
}

export interface LoginResponse {
  token: string;
  user: User;
}