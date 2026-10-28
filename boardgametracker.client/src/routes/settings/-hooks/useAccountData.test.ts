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
	removeProfileImageCall: vi.fn(),
	resetPasswordCall: vi.fn(),
	updateProfileCall: vi.fn(),
	updateUserCall: vi.fn(),
	uploadProfileImageCall: vi.fn(),
}));

import {
	deleteUserCall,
	removeProfileImageCall,
	updateProfileCall,
	updateUserCall,
	uploadProfileImageCall,
} from "@/services/authService";
import { useAccountData } from "./useAccountData";

const signedIn = {
	id: "u1",
	username: "admin",
	displayName: "Administrator",
	roles: ["Admin"],
	profileImage: null,
	needsPlayerLink: true,
};

const userDto = (overrides: { id?: string; username?: string; roles?: string[]; playerId?: number | null }) => ({
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
	profileImage: null,
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
			profileImage: null,
			needsPlayerLink: false,
		});
		const { result } = renderHook(() => useAccountData(), { wrapper: createWrapper() });

		await act(() =>
			result.current.updateProfile({ displayName: "Reggi", email: null, playerId: null, currentPassword: null }),
		);

		expect(useAuth.getState().user?.displayName).toBe("Reggi");
		expect(useAuth.getState().user?.needsPlayerLink).toBe(false);
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

const profile = (profileImage: string | null) => ({
	id: "u1",
	username: "admin",
	email: null,
	displayName: "Administrator",
	roles: ["Admin"],
	createdAt: new Date(),
	lastLoginAt: null,
	playerId: null,
	profileImage,
	needsPlayerLink: false,
});

describe("useAccountData profile picture", () => {
	beforeEach(() => {
		vi.clearAllMocks();
		useAuth.getState().setSession("access", signedIn);
	});

	it("shows an uploaded picture in the signed-in user right away", async () => {
		vi.mocked(uploadProfileImageCall).mockResolvedValue(profile("/images/profile/new.webp"));
		const { result } = renderHook(() => useAccountData(), { wrapper: createWrapper() });

		await act(() => result.current.uploadProfileImage(new File(["png"], "me.png", { type: "image/png" })));

		expect(useAuth.getState().user?.profileImage).toBe("/images/profile/new.webp");
		expect(successToast).toHaveBeenCalledWith("settings:account.notifications.picture-updated");
	});

	it("drops a removed picture from the signed-in user", async () => {
		useAuth.getState().updateUser({ profileImage: "/images/profile/old.webp" });
		vi.mocked(removeProfileImageCall).mockResolvedValue(profile(null));
		const { result } = renderHook(() => useAccountData(), { wrapper: createWrapper() });

		await act(() => result.current.removeProfileImage());

		expect(useAuth.getState().user?.profileImage).toBeNull();
		expect(successToast).toHaveBeenCalledWith("settings:account.notifications.picture-removed");
	});

	it("reports a failed upload and keeps the current picture", async () => {
		vi.mocked(uploadProfileImageCall).mockRejectedValue(new Error("too big"));
		const { result } = renderHook(() => useAccountData(), { wrapper: createWrapper() });

		await act(() =>
			result.current.uploadProfileImage(new File(["png"], "me.png", { type: "image/png" })).catch(() => undefined),
		);

		expect(useAuth.getState().user?.profileImage).toBeNull();
		expect(errorToast).toHaveBeenCalledWith("settings:account.notifications.picture-update-failed");
	});

	it("reports a failed removal", async () => {
		vi.mocked(removeProfileImageCall).mockRejectedValue(new Error("nope"));
		const { result } = renderHook(() => useAccountData(), { wrapper: createWrapper() });

		await act(() => result.current.removeProfileImage().catch(() => undefined));

		expect(errorToast).toHaveBeenCalledWith("settings:account.notifications.picture-update-failed");
	});
});

describe("useAccountData linked player refresh", () => {
	const setup = () => {
		const queryClient = new QueryClient({
			defaultOptions: { queries: { retry: false, gcTime: 0 }, mutations: { retry: false } },
		});
		const invalidate = vi.spyOn(queryClient, "invalidateQueries");
		const wrapper = ({ children }: { children: React.ReactNode }) =>
			React.createElement(QueryClientProvider, { client: queryClient }, children);
		const { result } = renderHook(() => useAccountData(), { wrapper });
		return { result, invalidate };
	};

	const expectPlayersRefreshed = (invalidate: ReturnType<typeof vi.spyOn>) => {
		expect(invalidate).toHaveBeenCalledWith({ queryKey: ["players"] });
		expect(invalidate).toHaveBeenCalledWith({ queryKey: ["player"] });
	};

	beforeEach(() => {
		vi.clearAllMocks();
		useAuth.getState().setSession("access", signedIn);
	});

	it("refreshes the players after a new account picture", async () => {
		vi.mocked(uploadProfileImageCall).mockResolvedValue(profile("/images/profile/new.webp"));
		const { result, invalidate } = setup();

		await act(() => result.current.uploadProfileImage(new File(["png"], "me.png", { type: "image/png" })));

		expectPlayersRefreshed(invalidate);
	});

	it("refreshes the players after removing the account picture", async () => {
		vi.mocked(removeProfileImageCall).mockResolvedValue(profile(null));
		const { result, invalidate } = setup();

		await act(() => result.current.removeProfileImage());

		expectPlayersRefreshed(invalidate);
	});

	it("refreshes the players after changing the linked player", async () => {
		vi.mocked(updateProfileCall).mockResolvedValue({ ...profile(null), playerId: 3 });
		const { result, invalidate } = setup();

		await act(() =>
			result.current.updateProfile({ displayName: "Administrator", email: null, playerId: 3, currentPassword: null }),
		);

		expectPlayersRefreshed(invalidate);
	});

	it("refreshes the players after an admin changes a link", async () => {
		vi.mocked(updateUserCall).mockResolvedValue(userDto({ id: "u2", username: "lotte", roles: ["User"] }));
		const { result, invalidate } = setup();

		await act(() =>
			result.current.updateUser({ userId: "u2", username: "lotte", email: null, role: "User", playerId: 4 }),
		);

		expectPlayersRefreshed(invalidate);
	});

	it("refreshes the profile and clears the link prompt when an admin links their own account", async () => {
		vi.mocked(updateUserCall).mockResolvedValue(userDto({ playerId: 7 }));
		const { result, invalidate } = setup();

		await act(() =>
			result.current.updateUser({ userId: "u1", username: "admin", email: null, role: "Admin", playerId: 7 }),
		);

		expect(invalidate).toHaveBeenCalledWith({ queryKey: ["profile"] });
		expect(useAuth.getState().user?.needsPlayerLink).toBe(false);
	});

	it("does not refresh the profile when an admin edits someone else", async () => {
		vi.mocked(updateUserCall).mockResolvedValue(userDto({ id: "u2", username: "lotte", roles: ["User"] }));
		const { result, invalidate } = setup();

		await act(() =>
			result.current.updateUser({ userId: "u2", username: "lotte", email: null, role: "User", playerId: null }),
		);

		expect(invalidate).not.toHaveBeenCalledWith({ queryKey: ["profile"] });
	});

	it("refreshes the user list after saving the profile", async () => {
		vi.mocked(updateProfileCall).mockResolvedValue(profile(null));
		const { result, invalidate } = setup();

		await act(() =>
			result.current.updateProfile({
				displayName: "Administrator",
				email: null,
				playerId: null,
				currentPassword: null,
			}),
		);

		expect(invalidate).toHaveBeenCalledWith({ queryKey: ["users"] });
	});

	it("refreshes the players after deleting an account", async () => {
		vi.mocked(deleteUserCall).mockResolvedValue(undefined);
		const { result, invalidate } = setup();

		await act(() => result.current.deleteUser("u2"));

		expectPlayersRefreshed(invalidate);
	});
});
