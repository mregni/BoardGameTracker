import axios, { isAxiosError } from "axios";
import { useAuth } from "@/hooks/useAuth";
import type { LoginResponse } from "@/models/Auth/Auth";
import { apiUrl } from "./apiUrl";
import { withRefreshLock } from "./refreshLock";
import { announceSessionChange, listenForSessionChanges } from "./sessionSync";

export const SESSION_HEADERS = { "X-Requested-With": "XMLHttpRequest" } as const;
export const SUPERSEDED_RETRY_DELAY_MS = 300;

const SUPERSEDED_ERROR = "error.auth.refresh-token-superseded";

let inFlight: Promise<string> | null = null;

const postRefresh = async (): Promise<LoginResponse> => {
	const response = await axios.post<LoginResponse>(`${apiUrl}auth/refresh`, null, {
		headers: SESSION_HEADERS,
		timeout: 30000,
	});
	return response.data;
};

const wasSuperseded = (error: unknown): boolean => {
	if (!isAxiosError(error) || error.response?.status !== 401) {
		return false;
	}
	const data = error.response.data as { title?: unknown } | null | undefined;
	return data?.title === SUPERSEDED_ERROR;
};

const refreshWithCookie = async (): Promise<LoginResponse> => {
	try {
		return await postRefresh();
	} catch (error) {
		if (!wasSuperseded(error)) {
			throw error;
		}
		await new Promise((resolve) => setTimeout(resolve, SUPERSEDED_RETRY_DELAY_MS));
		return postRefresh();
	}
};

export const refreshSession = (): Promise<string> => {
	inFlight ??= withRefreshLock(async () => {
		const { accessToken, user } = await refreshWithCookie();
		useAuth.getState().setSession(accessToken, user);
		announceSessionChange("refreshed");
		return accessToken;
	}).finally(() => {
		inFlight = null;
	});
	return inFlight;
};

export const restoreSession = async (): Promise<boolean> => {
	try {
		await refreshSession();
		return true;
	} catch {
		useAuth.getState().clearAuth();
		return false;
	}
};

export const startSessionSync = (): (() => void) =>
	listenForSessionChanges((change) => {
		if (change === "logout") {
			useAuth.getState().clearAuth();
			return;
		}
		if (change === "refreshed") {
			return;
		}
		refreshSession().catch(() => undefined);
	});
