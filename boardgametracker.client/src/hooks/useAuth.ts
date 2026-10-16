import { create } from "zustand";
import type { AuthStatus, LoginRequest, OidcProvider, User } from "@/models/Auth/Auth";
import { getAuthStatusCall, loginCall, logoutCall } from "@/services/authService";
import { announceSessionChange } from "@/utils/sessionSync";

export const LEGACY_AUTH_STORAGE_KEY = "bgt-auth";

export const removeLegacySession = () => {
	try {
		globalThis.localStorage?.removeItem(LEGACY_AUTH_STORAGE_KEY);
	} catch {
		return;
	}
};

removeLegacySession();

let authStatusRequest: Promise<AuthStatus> | null = null;

interface AuthState {
	accessToken: string | null;
	user: User | null;
	isAuthenticated: boolean;
	isLoading: boolean;
	authStatus: AuthStatus | null;
	oidcProvider: OidcProvider | null;

	login: (request: LoginRequest) => Promise<void>;
	logout: () => Promise<void>;
	setSession: (accessToken: string, user: User) => void;
	hasRole: (role: string) => boolean;
	fetchAuthStatus: () => Promise<AuthStatus>;
	clearAuth: () => void;
}

const signedOut = {
	accessToken: null,
	user: null,
	isAuthenticated: false,
};

export const useAuth = create<AuthState>()((set, get) => ({
	...signedOut,
	isLoading: false,
	authStatus: null,
	oidcProvider: null,

	login: async (request: LoginRequest) => {
		set({ isLoading: true });
		try {
			const response = await loginCall(request);
			set({
				accessToken: response.accessToken,
				user: response.user,
				isAuthenticated: true,
				isLoading: false,
			});
			announceSessionChange("login");
		} catch (error) {
			set({ isLoading: false });
			throw error;
		}
	},

	logout: async () => {
		try {
			await logoutCall();
		} finally {
			set(signedOut);
			announceSessionChange("logout");
		}
	},

	setSession: (accessToken: string, user: User) => {
		set({ accessToken, user, isAuthenticated: true });
	},

	hasRole: (role: string) => {
		const { user } = get();
		return user?.roles.includes(role) ?? false;
	},

	fetchAuthStatus: () => {
		authStatusRequest ??= getAuthStatusCall()
			.then((status) => {
				set({ authStatus: status });
				return status;
			})
			.finally(() => {
				authStatusRequest = null;
			});
		return authStatusRequest;
	},

	clearAuth: () => {
		set(signedOut);
	},
}));
