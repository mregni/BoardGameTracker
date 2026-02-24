import { create } from "zustand";
import { persist } from "zustand/middleware";
import type { AuthStatus, LoginRequest, OidcProvider, User } from "@/models/Auth/Auth";
import { getAuthStatusCall, getOidcProvidersCall, loginCall, logoutCall } from "@/services/authService";

interface AuthState {
	accessToken: string | null;
	refreshToken: string | null;
	user: User | null;
	isAuthenticated: boolean;
	isLoading: boolean;
	authStatus: AuthStatus | null;
	oidcProviders: OidcProvider[];

	login: (request: LoginRequest) => Promise<void>;
	logout: () => Promise<void>;
	setTokens: (accessToken: string, refreshToken: string, user: User) => void;
	hasRole: (role: string) => boolean;
	fetchAuthStatus: () => Promise<AuthStatus>;
	fetchOidcProviders: () => Promise<void>;
	clearAuth: () => void;
}

export const useAuth = create<AuthState>()(
	persist(
		(set, get) => ({
			accessToken: null,
			refreshToken: null,
			user: null,
			isAuthenticated: false,
			isLoading: false,
			authStatus: null,
			oidcProviders: [],

			login: async (request: LoginRequest) => {
				set({ isLoading: true });
				try {
					const response = await loginCall(request);
					set({
						accessToken: response.accessToken,
						refreshToken: response.refreshToken,
						user: response.user,
						isAuthenticated: true,
						isLoading: false,
					});
				} catch {
					set({ isLoading: false });
					throw new Error("Login failed");
				}
			},

			logout: async () => {
				const { refreshToken } = get();
				try {
					if (refreshToken) {
						await logoutCall(refreshToken);
					}
				} finally {
					set({
						accessToken: null,
						refreshToken: null,
						user: null,
						isAuthenticated: false,
					});
				}
			},

			setTokens: (accessToken: string, refreshToken: string, user: User) => {
				set({
					accessToken,
					refreshToken,
					user,
					isAuthenticated: true,
				});
			},

			hasRole: (role: string) => {
				const { user } = get();
				return user?.roles.includes(role) ?? false;
			},

			fetchAuthStatus: async () => {
				const status = await getAuthStatusCall();
				set({ authStatus: status });
				return status;
			},

			fetchOidcProviders: async () => {
				const providers = await getOidcProvidersCall();
				set({ oidcProviders: providers });
			},

			clearAuth: () => {
				set({
					accessToken: null,
					refreshToken: null,
					user: null,
					isAuthenticated: false,
				});
			},
		}),
		{
			name: "bgt-auth",
			partialize: (state) => ({
				accessToken: state.accessToken,
				refreshToken: state.refreshToken,
				user: state.user,
				isAuthenticated: state.isAuthenticated,
			}),
		},
	),
);
