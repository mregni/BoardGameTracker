import type { AuthStatus, LoginRequest, LoginResponse, OidcProvider, User } from "@/models/Auth/Auth";
import { axiosInstance } from "../utils/axiosInstance";

const domain = "auth";

export const loginCall = (request: LoginRequest): Promise<LoginResponse> => {
	return axiosInstance.post<LoginResponse>(`${domain}/login`, request).then((response) => {
		return response.data;
	});
};

export const logoutCall = (refreshToken?: string): Promise<void> => {
	return axiosInstance.post(`${domain}/logout`, { refreshToken });
};

export const refreshCall = (refreshToken: string): Promise<LoginResponse> => {
	return axiosInstance.post<LoginResponse>(`${domain}/refresh`, { refreshToken }).then((response) => {
		return response.data;
	});
};

export const getMeCall = (): Promise<User> => {
	return axiosInstance.get<User>(`${domain}/me`).then((response) => {
		return response.data;
	});
};

export const getAuthStatusCall = (): Promise<AuthStatus> => {
	return axiosInstance.get<AuthStatus>(`${domain}/status`).then((response) => {
		return response.data;
	});
};

export const getOidcProvidersCall = (): Promise<OidcProvider[]> => {
	return axiosInstance.get<OidcProvider[]>(`${domain}/oidc/providers`).then((response) => {
		return response.data;
	});
};
