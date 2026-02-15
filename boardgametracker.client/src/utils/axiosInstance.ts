import axios, { AxiosError } from 'axios';

import { ApiError, ApiErrorKind } from '@/models';

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

function classifyError(error: AxiosError): ApiError {
  const url = error.config?.url;

  if (error.code === 'ERR_NETWORK' || (!error.response && error.request)) {
    return { kind: 'network', status: null, message: 'Network error', url };
  }

  if (error.code === 'ECONNABORTED' || error.code === 'ETIMEDOUT') {
    return { kind: 'timeout', status: null, message: 'Request timed out', url };
  }

  if (error.response) {
    const status = error.response.status;
    const data = error.response.data as Record<string, unknown> | string | null;

    let message = 'An unexpected error occurred';
    if (data && typeof data === 'object' && 'reason' in data && typeof data.reason === 'string') {
      message = data.reason;
    } else if (data && typeof data === 'object' && 'title' in data && typeof data.title === 'string') {
      message = data.title;
    } else if (typeof data === 'string' && data.length > 0) {
      message = data;
    }

    const kind: ApiErrorKind = status >= 500 ? 'server' : 'client';
    return { kind, status, message, url };
  }

  return { kind: 'unknown', status: null, message: error.message || 'An unexpected error occurred', url };
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
  (error: AxiosError) => {
    return Promise.reject(classifyError(error));
  }
);
