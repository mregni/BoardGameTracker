export interface User {
	id: string;
	username: string;
	displayName: string | null;
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
	displayName: string;
	iconUrl: string | null;
	buttonColor: string | null;
}

export interface AuthStatus {
	authEnabled: boolean;
	bypassEnabled: boolean;
}

export interface ProfileResponse {
	id: string;
	username: string;
	email: string | null;
	displayName: string | null;
	roles: string[];
	createdAt: Date;
	lastLoginAt: Date | null;
	playerId: number | null;
}
