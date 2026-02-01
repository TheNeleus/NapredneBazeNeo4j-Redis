import axios from 'axios';

const apiClient = axios.create({
  baseURL: 'https://localhost:7000/api', 
  headers: {
    'Content-Type': 'application/json',
  },
});

// interceptor: Pre svakog slanja, ubaci token iz localStorage
apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem('meetup_token');
  if (token) {
    config.headers['Authorization'] = `Bearer ${token}`;
  }
  return config;
});

export default apiClient;