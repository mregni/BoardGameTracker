import axios, { type AxiosError } from "axios";

import type { ApiError, ApiErrorKind } from "@/models";

import { apiUrl } from "./apiUrl";

const isoDatePattern = /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d{3})?Z?$/;

// biome-ignore lint/suspicious/noExplicitAny: recursive date converter needs any for generic object traversal
function convertDatesInObject(obj: any): any {
  if (obj === null || obj === undefined) {
    return obj;
  }

  if (typeof obj === "string" && isoDatePattern.test(obj)) {
    return new Date(obj);
  }

  if (Array.isArray(obj)) {
    return obj.map(convertDatesInObject);
  }

  if (typeof obj === "object") {
    // biome-ignore lint/suspicious/noExplicitAny: recursive date converter needs any for generic object traversal
    const converted: any = {};
    for (const key in obj) {
      if (Object.hasOwn(obj, key)) {
        converted[key] = convertDatesInObject(obj[key]);
      }
    }
    return converted;
  }

  return obj;
}

function classifyError(error: AxiosError): ApiError {
  const url = error.config?.url;

  if (error.code === "ERR_NETWORK" || (!error.response && error.request)) {
    return { kind: "network", status: null, message: "Network error", url };
  }

  if (error.code === "ECONNABORTED" || error.code === "ETIMEDOUT") {
    return { kind: "timeout", status: null, message: "Request timed out", url };
  }

  if (error.response) {
    const status = error.response.status;
    const data = error.response.data as Record<string, unknown> | string | null;

    let message = "An unexpected error occurred";
    if (
      data &&
      typeof data === "object" &&
      "reason" in data &&
      typeof data.reason === "string"
    ) {
      message = data.reason;
    } else if (
      data &&
      typeof data === "object" &&
      "title" in data &&
      typeof data.title === "string"
    ) {
      message = data.title;
    } else if (typeof data === "string" && data.length > 0) {
      message = data;
    }

    const kind: ApiErrorKind = status >= 500 ? "server" : "client";
    return { kind, status, message, url };
  }

  return {
    kind: "unknown",
    status: null,
    message: error.message || "An unexpected error occurred",
    url,
  };
}

export const axiosInstance = axios.create({
  baseURL: apiUrl,
  timeout: 30000,
  headers: {
    "Content-Type": "application/json",
  },
});

// Track refresh state to avoid multiple simultaneous refresh attempts
let isRefreshing = false;
let failedQueue: Array<{
  resolve: (value: unknown) => void;
  reject: (reason: unknown) => void;
}> = [];

function processQueue(error: unknown, token: string | null = null) {
  for (const promise of failedQueue) {
    if (error) {
      promise.reject(error);
    } else {
      promise.resolve(token);
    }
  }
  failedQueue = [];
}

// Request interceptor: attach JWT token
axiosInstance.interceptors.request.use(
  (config) => {
    // Import dynamically to avoid circular dependency
    const authStorage = localStorage.getItem("bgt-auth");
    if (authStorage) {
      try {
        const parsed = JSON.parse(authStorage);
        const accessToken = parsed?.state?.accessToken;
        if (accessToken) {
          config.headers.Authorization = `Bearer ${accessToken}`;
        }
      } catch {
        // Ignore parse errors
      }
    }
    return config;
  },
  (error) => Promise.reject(error),
);

axiosInstance.interceptors.response.use(
  (response) => {
    if (response.data) {
      response.data = convertDatesInObject(response.data);
    }
    return response;
  },
  async (error: AxiosError) => {
    const originalRequest = error.config;

    if (
      error.response?.status === 401 &&
      originalRequest &&
      !originalRequest.url?.includes("auth/login") &&
      !originalRequest.url?.includes("auth/refresh") &&
      !originalRequest.url?.includes("auth/status") &&
      !(originalRequest as { _retry?: boolean })._retry
    ) {
      if (isRefreshing) {
        return new Promise((resolve, reject) => {
          failedQueue.push({ resolve, reject });
        }).then((token) => {
          if (originalRequest.headers) {
            originalRequest.headers.Authorization = `Bearer ${token}`;
          }
          return axiosInstance(originalRequest);
        });
      }

      (originalRequest as { _retry?: boolean })._retry = true;
      isRefreshing = true;

      const authStorage = localStorage.getItem("bgt-auth");
      let refreshToken: string | null = null;
      if (authStorage) {
        try {
          const parsed = JSON.parse(authStorage);
          refreshToken = parsed?.state?.refreshToken;
        } catch {
          // Ignore parse errors
        }
      }

      if (!refreshToken) {
        isRefreshing = false;
        processQueue(error, null);
        clearAuthState();
        return Promise.reject(classifyError(error));
      }

      try {
        const response = await axios.post(`${apiUrl}auth/refresh`, {
          refreshToken,
        });
        const {
          accessToken: newAccessToken,
          refreshToken: newRefreshToken,
          user,
        } = response.data;

        // Update zustand persisted state
        const currentStorage = localStorage.getItem("bgt-auth");
        if (currentStorage) {
          const parsed = JSON.parse(currentStorage);
          parsed.state.accessToken = newAccessToken;
          parsed.state.refreshToken = newRefreshToken;
          parsed.state.user = user;
          localStorage.setItem("bgt-auth", JSON.stringify(parsed));
        }

        if (originalRequest.headers) {
          originalRequest.headers.Authorization = `Bearer ${newAccessToken}`;
        }

        processQueue(null, newAccessToken);
        return axiosInstance(originalRequest);
      } catch (refreshError) {
        processQueue(refreshError, null);
        clearAuthState();
        window.location.href = "/login";
        return Promise.reject(classifyError(error));
      } finally {
        isRefreshing = false;
      }
    }

    return Promise.reject(classifyError(error));
  },
);

function clearAuthState() {
  const authStorage = localStorage.getItem("bgt-auth");
  if (authStorage) {
    try {
      const parsed = JSON.parse(authStorage);
      parsed.state.accessToken = null;
      parsed.state.refreshToken = null;
      parsed.state.user = null;
      parsed.state.isAuthenticated = false;
      localStorage.setItem("bgt-auth", JSON.stringify(parsed));
    } catch {
      localStorage.removeItem("bgt-auth");
    }
  }
}
