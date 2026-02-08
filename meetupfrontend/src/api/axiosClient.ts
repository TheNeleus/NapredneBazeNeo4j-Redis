import axios from 'axios';



const apiClient = axios.create({
  baseURL: 'https://localhost:7000/api', 
  headers: {
    'Content-Type': 'application/json',
  },
});

apiClient.interceptors.request.use((config) => {
  const token = sessionStorage.getItem('meetup_token');
  if (token) {
    config.headers['Authorization'] = `Bearer ${token}`;
  }
  return config;
});

apiClient.interceptors.response.use(
  (response) => {
    return response;
  },
  (error) => {
    if (error.response && error.response.status === 401) {
      console.warn("Session expired. Logging out user...");
      
      sessionStorage.removeItem('meetup_token');
      sessionStorage.removeItem('meetup_user');

      window.location.href = '/'; 
    }
    return Promise.reject(error);
  }
);

export default apiClient;