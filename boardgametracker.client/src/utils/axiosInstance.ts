import axios from 'axios';

import { apiUrl } from './apiUrl';

const isoDatePattern = /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d{3})?Z?$/;

// eslint-disable-next-line @typescript-eslint/no-explicit-any
function convertDatesInObject(obj: any): any {
  if (obj === null || obj === undefined) {
    return obj;
  }

  if (typeof obj === 'string' && isoDatePattern.test(obj)) {
    return new Date(obj);
  }

  if (Array.isArray(obj)) {
    return obj.map(convertDatesInObject);
  }

  if (typeof obj === 'object') {
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    const converted: any = {};
    for (const key in obj) {
      if (Object.prototype.hasOwnProperty.call(obj, key)) {
        converted[key] = convertDatesInObject(obj[key]);
      }
    }
    return converted;
  }

  return obj;
}

export const axiosInstance = axios.create({
  baseURL: apiUrl,
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json',
  },
});

axiosInstance.interceptors.response.use(
  (response) => {
    if (response.data) {
      response.data = convertDatesInObject(response.data);
    }
    return response;
  },
  (error) => {
    return Promise.reject(error);
  }
);
