export interface User {
	id: string;
	username: string;
	roles: string[];
}

export interface LoginRequest {
	username: string;
	password: string;
}

export interface LoginResponse {
	accessToken: string;
	refreshToken: string;
	expiresAt: Date;
	user: User;
}

export interface OidcProvider {
	name: string;
	iconUrl: string | null;
	buttonColor: string | null;
}

export interface AuthStatus {
	authEnabled: boolean;
}

export interface ProfileResponse {
	id: string;
	username: string;
	email: string | null;
	roles: string;
	createdAt: Date;
	lastLoginAt: Date | null;
	playerId: number | null;
}

export interface UpdateProfileRequest {
	username: string;
	email: string | null;
}

export interface ChangePasswordRequest {
	currentPassword: string;
	newPassword: string;
}

export interface RegisterRequest {
	username: string;
	email: string;
	password: string;
	role: string;
}

export interface UserDto {
	id: string;
	username: string;
	email: string | null;
	roles: string[];
	createdAt: Date;
	lastLoginAt: Date | null;
	playerId: number | null;
}

export interface ResetPasswordResponse {
	tempPassword: string;
}

export interface AdminUpdateUserRequest {
	username: string;
	email: string | null;
	role: string;
}
