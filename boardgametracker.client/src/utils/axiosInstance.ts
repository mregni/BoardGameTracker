import axios, { type AxiosError, type AxiosResponse, type InternalAxiosRequestConfig } from "axios";

import { useAuth } from "@/hooks/useAuth";
import type { ApiError, ApiErrorKind } from "@/models";

import { apiUrl } from "./apiUrl";

const isoDatePattern = /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d{1,7})?(Z|[+-]\d{2}:\d{2})?$/;

// biome-ignore lint/suspicious/noExplicitAny: recursive date converter needs any for generic object traversal
function convertDatesInObject(obj: any): any {
	if (obj === null || obj === undefined) {
		return obj;
	}

	if (obj instanceof Blob || obj instanceof ArrayBuffer) {
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
		if (data && typeof data === "object" && "reason" in data && typeof data.reason === "string") {
			message = data.reason;
		} else if (data && typeof data === "object" && "title" in data && typeof data.title === "string") {
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

const isApiRequest = (url?: string) => {
	if (!url) {
		return true;
	}

	if (!/^https?:\/\//i.test(url)) {
		return true;
	}

	return url.startsWith(apiUrl);
};

// Request interceptor: attach JWT token
axiosInstance.interceptors.request.use(
	(config) => {
		const accessToken = useAuth.getState().accessToken;
		if (accessToken && isApiRequest(config.url)) {
			config.headers.Authorization = `Bearer ${accessToken}`;
		}
		return config;
	},
	(error) => Promise.reject(error),
);

function getStoredRefreshToken(): string | null {
	return useAuth.getState().refreshToken;
}

async function handleTokenRefresh(
	originalRequest: InternalAxiosRequestConfig,
	error: AxiosError,
): Promise<AxiosResponse> {
	(originalRequest as { _retry?: boolean })._retry = true;
	isRefreshing = true;

	const refreshToken = getStoredRefreshToken();
	if (!refreshToken) {
		isRefreshing = false;
		processQueue(error, null);
		clearAuthState();
		throw classifyError(error);
	}

	try {
		const response = await axios.post(`${apiUrl}auth/refresh`, { refreshToken });
		const { accessToken: newAccessToken, refreshToken: newRefreshToken, user } = response.data;
		useAuth.getState().setTokens(newAccessToken, newRefreshToken, user);

		if (originalRequest.headers) {
			originalRequest.headers.Authorization = `Bearer ${newAccessToken}`;
		}

		processQueue(null, newAccessToken);
		return axiosInstance(originalRequest);
	} catch (refreshError) {
		processQueue(refreshError, null);
		clearAuthState();
		const currentPath = globalThis.location.pathname + globalThis.location.search;
		globalThis.location.href = `/login?redirect=${encodeURIComponent(currentPath)}`;
		throw classifyError(error);
	} finally {
		isRefreshing = false;
	}
}

function isUnauthorizedRetryable(error: AxiosError): boolean {
	const originalRequest = error.config;
	return (
		error.response?.status === 401 &&
		!!originalRequest &&
		!originalRequest.url?.includes("auth/login") &&
		!originalRequest.url?.includes("auth/refresh") &&
		!originalRequest.url?.includes("auth/status") &&
		!(originalRequest as { _retry?: boolean })._retry
	);
}

axiosInstance.interceptors.response.use(
	(response) => {
		if (response.data) {
			response.data = convertDatesInObject(response.data);
		}
		return response;
	},
	async (error: AxiosError) => {
		const originalRequest = error.config;

		if (originalRequest && isUnauthorizedRetryable(error)) {
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

			return handleTokenRefresh(originalRequest, error);
		}

		throw classifyError(error);
	},
);

function clearAuthState() {
	useAuth.getState().clearAuth();
}
