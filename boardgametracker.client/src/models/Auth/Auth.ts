import { z } from "zod";

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

export interface UpdateProfileRequest {
	displayName: string | null;
	email: string | null;
	playerId: number | null;
}

export interface PlayerLink {
	id: number;
	name: string;
}

export interface ChangePasswordRequest {
	currentPassword: string;
	newPassword: string;
}

export interface ForgotPasswordRequest {
	username: string;
}

export interface ResetPasswordConfirmRequest {
	userId: string;
	token: string;
	newPassword: string;
}

export interface RegisterRequest {
	username: string;
	email: string;
	password: string;
	role: string;
	createPlayer?: boolean;
	playerId?: number | null;
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
	playerId: number | null;
}

const requiredString = z.string().min(1, { message: "common:required" });
const passwordString = requiredString.min(4, { message: "settings:account.password.min-length" });

export const ChangePasswordSchema = z.object({
	currentPassword: requiredString,
	newPassword: passwordString,
	confirmPassword: z.string(),
});

export const CreateUserSchema = z.object({
	username: requiredString,
	email: requiredString,
	password: passwordString,
});

export const ResetPasswordSchema = z.object({
	newPassword: passwordString,
	confirmPassword: z.string(),
});
