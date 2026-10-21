import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { act, renderHook } from "@testing-library/react";
import React from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { useAuth } from "@/hooks/useAuth";

const { successToast, errorToast } = vi.hoisted(() => ({
	successToast: vi.fn(),
	errorToast: vi.fn(),
}));

vi.mock("@/routes/-hooks/useToasts", () => ({
	useToasts: () => ({ successToast, errorToast }),
}));

vi.mock("@/utils/errorUtils", () => ({
	apiErrorMessage: (_error: unknown, fallbackKey: string) => fallbackKey,
}));

vi.mock("@/services/queries/auth", () => ({
	getProfile: () => ({ queryKey: ["profile"], queryFn: () => new Promise(() => {}) }),
	getUsers: () => ({ queryKey: ["users"], queryFn: () => new Promise(() => {}) }),
	getLinkablePlayers: () => ({ queryKey: ["linkablePlayers"], queryFn: () => new Promise(() => {}) }),
}));

vi.mock("@/services/queries/players", () => ({
	getPlayers: () => ({ queryKey: ["players"], queryFn: () => new Promise(() => {}) }),
}));

vi.mock("@/services/authService", () => ({
	changePasswordCall: vi.fn(),
	deleteUserCall: vi.fn(),
	registerUserCall: vi.fn(),
	resetPasswordCall: vi.fn(),
	updateProfileCall: vi.fn(),
	updateUserCall: vi.fn(),
}));

import { updateProfileCall, updateUserCall } from "@/services/authService";
import { useAccountData } from "./useAccountData";

const signedIn = { id: "u1", username: "admin", displayName: "Administrator", roles: ["Admin"] };

const userDto = (overrides: { id?: string; username?: string; roles?: string[] }) => ({
	id: "u1",
	username: "admin",
	email: null,
	roles: ["Admin"],
	createdAt: new Date(),
	lastLoginAt: null,
	playerId: null,
	isDisabled: false,
	roleManagedByIdentityProvider: false,
	externalLoginCount: 0,
	...overrides,
});

const createWrapper = () => {
	const queryClient = new QueryClient({
		defaultOptions: { queries: { retry: false, gcTime: 0 }, mutations: { retry: false } },
	});
	return ({ children }: { children: React.ReactNode }) =>
		React.createElement(QueryClientProvider, { client: queryClient }, children);
};

describe("useAccountData", () => {
	beforeEach(() => {
		vi.clearAllMocks();
		useAuth.getState().setSession("access", signedIn);
	});

	it("shows a new display name in the signed-in user right after saving the profile", async () => {
		vi.mocked(updateProfileCall).mockResolvedValue({
			id: "u1",
			username: "admin",
			email: null,
			displayName: "Reggi",
			roles: ["Admin"],
			createdAt: new Date(),
			lastLoginAt: null,
			playerId: null,
		});
		const { result } = renderHook(() => useAccountData(), { wrapper: createWrapper() });

		await act(() =>
			result.current.updateProfile({ displayName: "Reggi", email: null, playerId: null, currentPassword: null }),
		);

		expect(useAuth.getState().user?.displayName).toBe("Reggi");
		expect(successToast).toHaveBeenCalledWith("settings:account.notifications.profile-updated");
	});

	it("updates the signed-in user when an admin edits their own account", async () => {
		vi.mocked(updateUserCall).mockResolvedValue(userDto({ username: "reggi" }));
		const { result } = renderHook(() => useAccountData(), { wrapper: createWrapper() });

		await act(() =>
			result.current.updateUser({ userId: "u1", username: "reggi", email: null, role: "Admin", playerId: null }),
		);

		expect(useAuth.getState().user).toEqual({ ...signedIn, username: "reggi" });
	});

	it("leaves the signed-in user alone when an admin edits someone else", async () => {
		vi.mocked(updateUserCall).mockResolvedValue(userDto({ id: "u2", username: "lotte", roles: ["User"] }));
		const { result } = renderHook(() => useAccountData(), { wrapper: createWrapper() });

		await act(() =>
			result.current.updateUser({ userId: "u2", username: "lotte", email: null, role: "User", playerId: null }),
		);

		expect(useAuth.getState().user).toEqual(signedIn);
	});
});
