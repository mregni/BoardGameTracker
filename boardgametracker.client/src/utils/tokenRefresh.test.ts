import axios, { AxiosError, AxiosHeaders } from "axios";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { useAuth } from "@/hooks/useAuth";
import type { User } from "@/models/Auth/Auth";

const sync = vi.hoisted(() => ({
	handler: null as ((change: "login" | "logout" | "refreshed") => void) | null,
	stop: vi.fn(),
	announce: vi.fn(),
}));

vi.mock("./sessionSync", () => ({
	announceSessionChange: sync.announce,
	listenForSessionChanges: (handler: (change: "login" | "logout" | "refreshed") => void) => {
		sync.handler = handler;
		return sync.stop;
	},
}));

import { refreshSession, restoreSession, SESSION_HEADERS, startSessionSync } from "./tokenRefresh";

const user: User = { id: "u1", username: "alice", displayName: null, roles: ["User"], profileImage: null };
const admin: User = { id: "u2", username: "bob", displayName: null, roles: ["Admin"], profileImage: null };

const rejection = (status: number, title?: string) =>
	new AxiosError("failed", "ERR_BAD_REQUEST", undefined, undefined, {
		status,
		statusText: "",
		data: title ? { title } : {},
		headers: {},
		config: { headers: new AxiosHeaders() },
	});

describe("tokenRefresh", () => {
	beforeEach(() => {
		localStorage.clear();
		sessionStorage.clear();
		useAuth.getState().setSession("access-1", user);
	});

	afterEach(() => {
		vi.useRealTimers();
		vi.restoreAllMocks();
		vi.unstubAllGlobals();
	});

	it("refreshes with the cookie only, sending no token and the anti-forgery header", async () => {
		const post = vi.spyOn(axios, "post").mockResolvedValue({ data: { accessToken: "access-2", user } });

		const accessToken = await refreshSession();

		expect(accessToken).toBe("access-2");
		expect(post).toHaveBeenCalledWith("/api/auth/refresh", null, expect.objectContaining({ headers: SESSION_HEADERS }));
		expect(SESSION_HEADERS).toEqual({ "X-Requested-With": "XMLHttpRequest" });
		expect(useAuth.getState().accessToken).toBe("access-2");
		expect(localStorage.length + sessionStorage.length).toBe(0);
	});

	it("shares one refresh between callers in the same tab", async () => {
		let finish: (value: unknown) => void = () => {};
		const post = vi.spyOn(axios, "post").mockReturnValue(new Promise((resolve) => (finish = resolve)));

		const first = refreshSession();
		const second = refreshSession();
		finish({ data: { accessToken: "access-2", user } });

		await expect(Promise.all([first, second])).resolves.toEqual(["access-2", "access-2"]);
		expect(post).toHaveBeenCalledTimes(1);
	});

	it("serialises refreshes across tabs with a Web Lock when the browser has one", async () => {
		const request = vi.fn((_name: string, task: () => Promise<unknown>) => task());
		vi.stubGlobal("navigator", { ...navigator, locks: { request } });
		vi.spyOn(axios, "post").mockResolvedValue({ data: { accessToken: "access-2", user } });

		await refreshSession();

		expect(request).toHaveBeenCalledWith("bgt-token-refresh", expect.any(Function));
	});

	it("tells other tabs after a successful refresh", async () => {
		vi.spyOn(axios, "post").mockResolvedValue({ data: { accessToken: "access-2", user } });

		await refreshSession();

		expect(sync.announce).toHaveBeenCalledWith("refreshed");
	});

	it("retries once when another tab rotated the cookie at the same moment", async () => {
		vi.useFakeTimers();
		const post = vi
			.spyOn(axios, "post")
			.mockRejectedValueOnce(rejection(401, "error.auth.refresh-token-superseded"))
			.mockResolvedValueOnce({ data: { accessToken: "access-3", user } });

		const pending = refreshSession();
		await vi.runAllTimersAsync();

		await expect(pending).resolves.toBe("access-3");
		expect(post).toHaveBeenCalledTimes(2);
	});

	it("retries only once and then goes to sign-in", async () => {
		vi.useFakeTimers();
		const post = vi.spyOn(axios, "post").mockRejectedValue(rejection(401, "error.auth.refresh-token-superseded"));

		const pending = restoreSession();
		await vi.runAllTimersAsync();

		await expect(pending).resolves.toBe(false);
		expect(post).toHaveBeenCalledTimes(2);
		expect(useAuth.getState().isAuthenticated).toBe(false);
	});

	it("does not refresh again when another tab only reports its own refresh", () => {
		const post = vi.spyOn(axios, "post");
		startSessionSync();

		sync.handler?.("refreshed");

		expect(post).not.toHaveBeenCalled();
		expect(useAuth.getState().accessToken).toBe("access-1");
	});

	it("does not retry an expired or revoked session", async () => {
		const post = vi.spyOn(axios, "post").mockRejectedValue(rejection(401, "error.auth.invalid-refresh-token"));

		await expect(refreshSession()).rejects.toBeInstanceOf(AxiosError);
		expect(post).toHaveBeenCalledTimes(1);
		expect(useAuth.getState().accessToken).toBe("access-1");
	});

	it("does not retry a network failure", async () => {
		const post = vi.spyOn(axios, "post").mockRejectedValue(new Error("offline"));

		await expect(refreshSession()).rejects.toThrow("offline");
		expect(post).toHaveBeenCalledTimes(1);
	});

	it("does not retry a 401 without a body", async () => {
		const post = vi.spyOn(axios, "post").mockRejectedValue(rejection(401));

		await expect(refreshSession()).rejects.toBeInstanceOf(AxiosError);
		expect(post).toHaveBeenCalledTimes(1);
	});

	it("restoreSession reports success and keeps the user", async () => {
		useAuth.getState().clearAuth();
		vi.spyOn(axios, "post").mockResolvedValue({ data: { accessToken: "access-9", user } });

		await expect(restoreSession()).resolves.toBe(true);

		expect(useAuth.getState().isAuthenticated).toBe(true);
		expect(useAuth.getState().user).toEqual(user);
	});

	it("restoreSession signs out quietly when the cookie is missing or dead", async () => {
		vi.spyOn(axios, "post").mockRejectedValue(rejection(401, "error.auth.invalid-refresh-token"));

		await expect(restoreSession()).resolves.toBe(false);

		expect(useAuth.getState().isAuthenticated).toBe(false);
		expect(useAuth.getState().accessToken).toBeNull();
	});

	it("signs this tab out when another tab logs out", () => {
		const stop = startSessionSync();

		sync.handler?.("logout");

		expect(useAuth.getState().isAuthenticated).toBe(false);
		stop();
		expect(sync.stop).toHaveBeenCalled();
	});

	it("picks up the session another tab just signed in with", async () => {
		useAuth.getState().clearAuth();
		vi.spyOn(axios, "post").mockResolvedValue({ data: { accessToken: "access-other", user: admin } });
		startSessionSync();

		sync.handler?.("login");

		await vi.waitFor(() => expect(useAuth.getState().user).toEqual(admin));
		expect(useAuth.getState().accessToken).toBe("access-other");
	});

	it("keeps the current session when following another tab's login fails", async () => {
		vi.spyOn(axios, "post").mockRejectedValue(new Error("offline"));
		startSessionSync();

		sync.handler?.("login");

		await vi.waitFor(() => expect(axios.post).toHaveBeenCalled());
		expect(useAuth.getState().accessToken).toBe("access-1");
	});
});
