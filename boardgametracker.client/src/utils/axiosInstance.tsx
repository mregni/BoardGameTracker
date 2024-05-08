import axios from 'axios';

import { apiUrl } from './apiUrl';

export const axiosInstance = axios.create({
  baseURL: apiUrl,
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json',
  },
});
