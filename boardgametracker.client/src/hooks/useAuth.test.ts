import { beforeEach, describe, expect, it, vi } from "vitest";
import type { AuthStatus, User } from "@/models/Auth/Auth";

const authService = vi.hoisted(() => ({
	loginCall: vi.fn(),
	logoutCall: vi.fn(),
	getAuthStatusCall: vi.fn(),
}));

vi.mock("@/services/authService", () => authService);

import { useAuth } from "./useAuth";

const user: User = { id: "u1", username: "alice", displayName: null, roles: ["User"] };

describe("useAuth", () => {
	beforeEach(() => {
		vi.clearAllMocks();
		useAuth.getState().clearAuth();
	});

	it("login stores the tokens and the user", async () => {
		authService.loginCall.mockResolvedValue({ accessToken: "access", refreshToken: "refresh", user });

		await useAuth.getState().login({ username: "alice", password: "pw" });

		const state = useAuth.getState();
		expect(state.isAuthenticated).toBe(true);
		expect(state.accessToken).toBe("access");
		expect(state.refreshToken).toBe("refresh");
		expect(state.hasRole("User")).toBe(true);
		expect(state.hasRole("Admin")).toBe(false);
		expect(state.isLoading).toBe(false);
	});

	it("login resets the loading flag and rethrows when the call fails", async () => {
		authService.loginCall.mockRejectedValue(new Error("401"));

		await expect(useAuth.getState().login({ username: "alice", password: "bad" })).rejects.toThrow("Login failed");

		expect(useAuth.getState().isAuthenticated).toBe(false);
		expect(useAuth.getState().isLoading).toBe(false);
	});

	it("logout revokes the stored refresh token and clears the session even when the call fails", async () => {
		useAuth.getState().setTokens("access", "refresh", user);
		authService.logoutCall.mockRejectedValue(new Error("network"));

		await expect(useAuth.getState().logout()).rejects.toThrow("network");

		expect(authService.logoutCall).toHaveBeenCalledWith("refresh");
		expect(useAuth.getState().isAuthenticated).toBe(false);
		expect(useAuth.getState().accessToken).toBeNull();
	});

	it("logout skips the server call when there is no refresh token", async () => {
		await useAuth.getState().logout();

		expect(authService.logoutCall).not.toHaveBeenCalled();
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
