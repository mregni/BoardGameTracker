import { AxiosError, type AxiosResponse, type InternalAxiosRequestConfig } from "axios";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { useAuth } from "@/hooks/useAuth";
import type { User } from "@/models/Auth/Auth";

const mocks = vi.hoisted(() => ({
	refreshSession: vi.fn(),
}));

vi.mock("./tokenRefresh", () => ({
	refreshSession: () => mocks.refreshSession(),
}));

import { axiosInstance } from "./axiosInstance";

const user: User = {
	id: "u1",
	username: "alice",
	displayName: null,
	roles: ["User"],
	profileImage: null,
	needsPlayerLink: false,
};

type Handler = (config: InternalAxiosRequestConfig) => AxiosResponse | number;

const respond = (handler: Handler) => {
	const seen: string[] = [];
	axiosInstance.defaults.adapter = async (config) => {
		seen.push(String(config.headers.Authorization));
		const outcome = handler(config);
		if (typeof outcome === "number") {
			const response = { data: {}, status: outcome, statusText: "", headers: {}, config } as AxiosResponse;
			throw new AxiosError("failed", "ERR_BAD_REQUEST", config, null, response);
		}
		return outcome;
	};
	return seen;
};

const ok = (config: InternalAxiosRequestConfig, data: unknown = { ok: true }): AxiosResponse => ({
	data,
	status: 200,
	statusText: "OK",
	headers: {},
	config,
});

const bearer = (config: InternalAxiosRequestConfig) => config.headers.Authorization;

const rotateTo = (token: string) => () => {
	useAuth.getState().setSession(token, user);
	return Promise.resolve(token);
};

const sharedRefresh = () => {
	let started = 0;
	let waiting = 0;
	let inFlight: Promise<string> | null = null;
	let settle: { resolve: (token: string) => void; reject: (error: Error) => void } = {
		resolve: () => {},
		reject: () => {},
	};
	mocks.refreshSession.mockImplementation(() => {
		waiting += 1;
		if (!inFlight) {
			started += 1;
			inFlight = new Promise<string>((resolve, reject) => {
				settle = { resolve, reject };
			}).then((token) => {
				useAuth.getState().setSession(token, user);
				return token;
			});
		}
		return inFlight;
	});
	return {
		started: () => started,
		waiting: () => waiting,
		resolve: (token: string) => settle.resolve(token),
		reject: (error: Error) => settle.reject(error),
	};
};

describe("axiosInstance refresh on 401", () => {
	beforeEach(() => {
		vi.clearAllMocks();
		useAuth.getState().setSession("expired", user);
		vi.stubGlobal("location", { pathname: "/games", search: "?page=2", href: "/games?page=2" });
	});

	afterEach(() => {
		vi.unstubAllGlobals();
	});

	it("refreshes through the cookie and replays the request with the new access token", async () => {
		mocks.refreshSession.mockImplementation(rotateTo("fresh"));
		const seen = respond((config) => (bearer(config) === "Bearer fresh" ? ok(config) : 401));

		const response = await axiosInstance.get("games");

		expect(response.data).toEqual({ ok: true });
		expect(mocks.refreshSession).toHaveBeenCalledTimes(1);
		expect(seen).toEqual(["Bearer expired", "Bearer fresh"]);
	});

	it("replays concurrent 401s after one shared refresh", async () => {
		const gate = sharedRefresh();
		respond((config) => (bearer(config) === "Bearer fresh" ? ok(config, config.url) : 401));

		const requests = [axiosInstance.get("games"), axiosInstance.get("players"), axiosInstance.get("locations")];
		await vi.waitFor(() => expect(gate.waiting()).toBe(3));
		gate.resolve("fresh");

		const responses = await Promise.all(requests);
		expect(responses.map((r) => r.data)).toEqual(["games", "players", "locations"]);
		expect(gate.started()).toBe(1);
	});

	it("rejects every concurrent 401 with an ApiError when the shared refresh fails", async () => {
		const gate = sharedRefresh();
		const seen = respond(() => 401);

		const requests = [axiosInstance.get("games"), axiosInstance.get("players"), axiosInstance.get("locations")];
		await vi.waitFor(() => expect(gate.waiting()).toBe(3));
		gate.reject(new Error("refresh failed"));

		const results = await Promise.allSettled(requests);
		for (const result of results) {
			expect(result).toMatchObject({
				status: "rejected",
				reason: { kind: "client", status: 401, message: expect.any(String) },
			});
			expect((result as PromiseRejectedResult).reason).not.toBeInstanceOf(Error);
		}
		expect(gate.started()).toBe(1);
		expect(seen).toHaveLength(3);
		expect(useAuth.getState().accessToken).toBeNull();
	});

	it("signs out and goes to the login page when the cookie session is gone", async () => {
		mocks.refreshSession.mockRejectedValue(new Error("refresh failed"));
		respond(() => 401);

		await expect(axiosInstance.get("games")).rejects.toMatchObject({ kind: "client", status: 401 });

		expect(useAuth.getState().isAuthenticated).toBe(false);
		expect(useAuth.getState().accessToken).toBeNull();
		expect(globalThis.location.href).toBe(`/login?redirect=${encodeURIComponent("/games?page=2")}`);
	});

	it("leaves anonymous visitors alone instead of bouncing them to the login page", async () => {
		useAuth.getState().clearAuth();
		respond(() => 401);

		await expect(axiosInstance.get("game-nights/rsvp/abc")).rejects.toMatchObject({ status: 401 });

		expect(mocks.refreshSession).not.toHaveBeenCalled();
		expect(globalThis.location.href).toBe("/games?page=2");
	});

	it.each(["auth/login", "auth/refresh", "auth/status"])("does not refresh for %s", async (url) => {
		respond(() => 401);

		await expect(axiosInstance.post(url)).rejects.toMatchObject({ status: 401 });

		expect(mocks.refreshSession).not.toHaveBeenCalled();
	});

	it("gives up after one replay that is still unauthorised", async () => {
		mocks.refreshSession.mockResolvedValue("fresh");
		const seen = respond(() => 401);

		await expect(axiosInstance.get("games")).rejects.toMatchObject({ status: 401 });

		expect(seen).toHaveLength(2);
		expect(mocks.refreshSession).toHaveBeenCalledTimes(1);
	});
});
