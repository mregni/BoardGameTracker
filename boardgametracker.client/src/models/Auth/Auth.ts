import { z } from "zod";

export interface User {
	id: string;
	username: string;
	displayName: string | null;
	roles: string[];
	profileImage: string | null;
	needsPlayerLink: boolean;
}

export interface LoginRequest {
	username: string;
	password: string;
}

export interface LoginResponse {
	accessToken: string;
	expiresAt: Date;
	user: User;
}

export interface OidcProvider {
	displayName: string;
	iconUrl: string | null;
	buttonColor: string | null;
	httpsRequired: boolean;
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
	profileImage: string | null;
	needsPlayerLink: boolean;
}

export interface UpdateProfileRequest {
	displayName: string | null;
	email: string | null;
	playerId: number | null;
	currentPassword: string | null;
}

export interface PlayerLink {
	id: number;
	name: string;
}

export type PlayerLinkMatchReason = "email" | "name" | "partial";

export interface PlayerLinkCandidate {
	id: number;
	name: string;
	image: string | null;
}

export interface PlayerLinkSuggestion extends PlayerLinkCandidate {
	reason: PlayerLinkMatchReason;
}

export interface PlayerLinkSuggestions {
	suggestions: PlayerLinkSuggestion[];
	players: PlayerLinkCandidate[];
	canCreatePlayer: boolean;
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
	displayName?: string | null;
	roles: string[];
	createdAt: Date;
	lastLoginAt: Date | null;
	playerId: number | null;
	isDisabled: boolean;
	roleManagedByIdentityProvider: boolean;
	externalLoginCount: number;
	profileImage: string | null;
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
const passwordString = requiredString.min(8, { message: "settings:account.password.min-length" });
const invalidEmail = { message: "player:email.invalid" };

export const ChangePasswordSchema = z.object({
	currentPassword: requiredString,
	newPassword: passwordString,
	confirmPassword: z.string(),
});

export const CreateUserSchema = z.object({
	username: requiredString,
	email: requiredString.email(invalidEmail),
	password: passwordString,
});

export const EditUserSchema = z.object({
	username: z.string().min(1, { message: "error:auth.username-required" }),
	email: z.string().email(invalidEmail).or(z.literal("")),
});

export const ForgotPasswordSchema = z.object({
	username: z.string().trim().min(1, { message: "common:required" }),
});

export const ResetPasswordSchema = z.object({
	newPassword: passwordString,
	confirmPassword: z.string(),
});
