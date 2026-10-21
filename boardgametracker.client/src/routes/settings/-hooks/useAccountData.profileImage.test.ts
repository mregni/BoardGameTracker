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

import { removeProfileImageCall, uploadProfileImageCall } from "@/services/authService";
import { useAccountData } from "./useAccountData";

const signedIn = { id: "u1", username: "admin", displayName: "Administrator", roles: ["Admin"], profileImage: null };

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
});

const createWrapper = () => {
	const queryClient = new QueryClient({
		defaultOptions: { queries: { retry: false, gcTime: 0 }, mutations: { retry: false } },
	});
	return ({ children }: { children: React.ReactNode }) =>
		React.createElement(QueryClientProvider, { client: queryClient }, children);
};

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
