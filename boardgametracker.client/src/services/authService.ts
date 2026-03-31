import type {
	AdminUpdateUserRequest,
	AuthStatus,
	ChangePasswordRequest,
	LoginRequest,
	LoginResponse,
	OidcProvider,
	ProfileResponse,
	RegisterRequest,
	ResetPasswordResponse,
	UpdateProfileRequest,
	UserDto,
} from "@/models/Auth/Auth";
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

export const getAuthStatusCall = (): Promise<AuthStatus> => {
	return axiosInstance.get<AuthStatus>(`${domain}/status`).then((response) => {
		return response.data;
	});
};

export const getOidcProviderCall = (): Promise<OidcProvider | null> => {
	return axiosInstance
		.get<OidcProvider>(`${domain}/oidc/provider`)
		.then((response) => response.data)
		.catch((error) => {
			if (error.response?.status === 404) return null;
			throw error;
		});
};

export const getProfileCall = (): Promise<ProfileResponse> => {
	return axiosInstance.get<ProfileResponse>(`${domain}/profile`).then((response) => {
		return response.data;
	});
};

export const updateProfileCall = (request: UpdateProfileRequest): Promise<ProfileResponse> => {
	return axiosInstance.put<ProfileResponse>(`${domain}/profile`, request).then((response) => {
		return response.data;
	});
};

export const changePasswordCall = (request: ChangePasswordRequest): Promise<void> => {
	return axiosInstance.post(`${domain}/change-password`, request);
};

export const registerUserCall = (request: RegisterRequest): Promise<UserDto> => {
	return axiosInstance.post<UserDto>(`${domain}/register`, request).then((response) => {
		return response.data;
	});
};

export const resetPasswordCall = (userId: string): Promise<ResetPasswordResponse> => {
	return axiosInstance.post<ResetPasswordResponse>(`${domain}/reset-password/${userId}`).then((response) => {
		return response.data;
	});
};

export const getUsersCall = (): Promise<UserDto[]> => {
	return axiosInstance.get<UserDto[]>("admin/users").then((response) => {
		return response.data;
	});
};

export const deleteUserCall = (userId: string): Promise<void> => {
	return axiosInstance.delete(`admin/users/${userId}`);
};

export const updateUserRoleCall = (userId: string, role: string): Promise<UserDto> => {
	return axiosInstance.put<UserDto>(`admin/users/${userId}/role`, { role }).then((response) => response.data);
};

export const updateUserCall = (userId: string, request: AdminUpdateUserRequest): Promise<UserDto> => {
	return axiosInstance.put<UserDto>(`admin/users/${userId}`, request).then((response) => response.data);
};
