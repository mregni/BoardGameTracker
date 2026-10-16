import axios from "axios";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { AUTH_STORAGE_KEY, useAuth } from "@/hooks/useAuth";
import type { User } from "@/models/Auth/Auth";
import { refreshAccessToken, tokensRefreshedElsewhere } from "./tokenRefresh";

const user: User = { id: "u1", username: "alice", displayName: null, roles: ["User"] };

const writeStoredTokens = (accessToken: string, refreshToken: string) => {
	localStorage.setItem(
		AUTH_STORAGE_KEY,
		JSON.stringify({ state: { accessToken, refreshToken, user, isAuthenticated: true }, version: 0 }),
	);
};

describe("tokenRefresh", () => {
	beforeEach(() => {
		localStorage.clear();
		useAuth.getState().setTokens("access-1", "refresh-1", user);
	});

	afterEach(() => {
		vi.restoreAllMocks();
		vi.unstubAllGlobals();
	});

	it("refreshes with the current token and stores the rotated pair", async () => {
		const post = vi.spyOn(axios, "post").mockResolvedValue({
			data: { accessToken: "access-2", refreshToken: "refresh-2", user },
		});

		const accessToken = await refreshAccessToken("refresh-1");

		expect(accessToken).toBe("access-2");
		expect(post).toHaveBeenCalledWith(expect.stringContaining("auth/refresh"), { refreshToken: "refresh-1" });
		expect(useAuth.getState().refreshToken).toBe("refresh-2");
	});

	it("reuses the tokens another tab already rotated instead of presenting the old refresh token again", async () => {
		const post = vi.spyOn(axios, "post");
		writeStoredTokens("access-from-other-tab", "refresh-from-other-tab");

		const accessToken = await refreshAccessToken("refresh-1");

		expect(accessToken).toBe("access-from-other-tab");
		expect(post).not.toHaveBeenCalled();
		expect(useAuth.getState().refreshToken).toBe("refresh-from-other-tab");
	});

	it("serialises refreshes across tabs with a Web Lock when the browser has one", async () => {
		const request = vi.fn((_name: string, task: () => Promise<unknown>) => task());
		vi.stubGlobal("navigator", { ...navigator, locks: { request } });
		vi.spyOn(axios, "post").mockResolvedValue({ data: { accessToken: "access-2", refreshToken: "refresh-2", user } });

		await refreshAccessToken("refresh-1");

		expect(request).toHaveBeenCalledWith("bgt-token-refresh", expect.any(Function));
	});

	it("reports no newer tokens when storage still holds the stale refresh token", async () => {
		writeStoredTokens("access-1", "refresh-1");

		await expect(tokensRefreshedElsewhere("refresh-1")).resolves.toBeNull();
	});

	it("follows another tab through the storage event", async () => {
		writeStoredTokens("access-3", "refresh-3");

		window.dispatchEvent(new StorageEvent("storage", { key: AUTH_STORAGE_KEY }));

		await vi.waitFor(() => expect(useAuth.getState().refreshToken).toBe("refresh-3"));
	});
});
