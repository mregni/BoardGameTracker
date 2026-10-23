import { beforeEach, describe, expect, it, vi } from "vitest";
import type { AuthStatus, User } from "@/models/Auth/Auth";

const authService = vi.hoisted(() => ({
	loginCall: vi.fn(),
	logoutCall: vi.fn(),
	getAuthStatusCall: vi.fn(),
}));

const sessionSync = vi.hoisted(() => ({
	announceSessionChange: vi.fn(),
}));

vi.hoisted(() => {
	localStorage.setItem(
		"bgt-auth",
		JSON.stringify({ state: { accessToken: "old", refreshToken: "old-refresh", isAuthenticated: true }, version: 0 }),
	);
});

vi.mock("@/services/authService", () => authService);
vi.mock("@/utils/sessionSync", () => sessionSync);

import { LEGACY_AUTH_STORAGE_KEY, removeLegacySession, useAuth } from "./useAuth";

const user: User = {
	id: "u1",
	username: "alice",
	displayName: null,
	roles: ["User"],
	profileImage: null,
	needsPlayerLink: false,
};

const storedValues = () => [
	...Object.keys(localStorage).map((key) => localStorage.getItem(key) ?? ""),
	...Object.keys(sessionStorage).map((key) => sessionStorage.getItem(key) ?? ""),
];

describe("useAuth", () => {
	beforeEach(() => {
		vi.clearAllMocks();
		useAuth.getState().clearAuth();
	});

	it("drops the refresh token a previous version kept in localStorage", () => {
		expect(localStorage.getItem(LEGACY_AUTH_STORAGE_KEY)).toBeNull();
		expect(useAuth.getState().isAuthenticated).toBe(false);
		expect(useAuth.getState().accessToken).toBeNull();
	});

	it("tolerates storage that cannot be reached", () => {
		const removeItem = vi.spyOn(Storage.prototype, "removeItem").mockImplementation(() => {
			throw new Error("blocked");
		});

		expect(() => removeLegacySession()).not.toThrow();
		removeItem.mockRestore();
	});

	it("login keeps the access token in memory only and tells other tabs", async () => {
		authService.loginCall.mockResolvedValue({ accessToken: "access", expiresAt: new Date(), user });

		await useAuth.getState().login({ username: "alice", password: "pw" });

		const state = useAuth.getState();
		expect(state.isAuthenticated).toBe(true);
		expect(state.accessToken).toBe("access");
		expect(state).not.toHaveProperty("refreshToken");
		expect(state.hasRole("User")).toBe(true);
		expect(state.hasRole("Admin")).toBe(false);
		expect(state.isLoading).toBe(false);
		expect(storedValues().some((value) => value.includes("access"))).toBe(false);
		expect(sessionSync.announceSessionChange).toHaveBeenCalledWith("login");
	});

	it("login resets the loading flag and rethrows the original error when the call fails", async () => {
		const lockedOut = { kind: "client", status: 401, message: "error.auth.account-locked-out", url: "auth/login" };
		authService.loginCall.mockRejectedValue(lockedOut);

		await expect(useAuth.getState().login({ username: "alice", password: "bad" })).rejects.toBe(lockedOut);

		expect(useAuth.getState().isAuthenticated).toBe(false);
		expect(useAuth.getState().isLoading).toBe(false);
		expect(sessionSync.announceSessionChange).not.toHaveBeenCalled();
	});

	it("logout asks the server to revoke the cookie session and clears memory even when the call fails", async () => {
		useAuth.getState().setSession("access", user);
		authService.logoutCall.mockRejectedValue(new Error("network"));

		await expect(useAuth.getState().logout()).rejects.toThrow("network");

		expect(authService.logoutCall).toHaveBeenCalledWith();
		expect(useAuth.getState().isAuthenticated).toBe(false);
		expect(useAuth.getState().accessToken).toBeNull();
		expect(useAuth.getState().user).toBeNull();
		expect(sessionSync.announceSessionChange).toHaveBeenCalledWith("logout");
	});

	it("updateUser merges changes into the signed-in user", () => {
		useAuth.getState().setSession("access", user);

		useAuth.getState().updateUser({ displayName: "Reggi" });

		expect(useAuth.getState().user).toEqual({ ...user, displayName: "Reggi" });
		expect(useAuth.getState().accessToken).toBe("access");
	});

	it("updateUser does nothing while signed out", () => {
		useAuth.getState().clearAuth();

		useAuth.getState().updateUser({ displayName: "Reggi" });

		expect(useAuth.getState().user).toBeNull();
	});

	it("logout always calls the server because only the server can see the cookie", async () => {
		authService.logoutCall.mockResolvedValue(undefined);

		await useAuth.getState().logout();

		expect(authService.logoutCall).toHaveBeenCalledTimes(1);
	});

	it("fetchAuthStatus shares one in-flight request", async () => {
		const status: AuthStatus = { authEnabled: true };
		let resolve: (value: AuthStatus) => void = () => {};
		authService.getAuthStatusCall.mockReturnValue(new Promise<AuthStatus>((r) => (resolve = r)));

		const first = useAuth.getState().fetchAuthStatus();
		const second = useAuth.getState().fetchAuthStatus();
		resolve(status);
		await Promise.all([first, second]);

		expect(authService.getAuthStatusCall).toHaveBeenCalledTimes(1);
		expect(useAuth.getState().authStatus).toEqual(status);
	});
});
