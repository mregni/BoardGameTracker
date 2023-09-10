import axios from 'axios';

let baseURL = '/api/';
if (process.env.NODE_ENV === 'development') {
  baseURL = 'https://localhost:7178/api/';
}

export const axiosInstance = axios.create({
  baseURL: baseURL,
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json',
  },
});