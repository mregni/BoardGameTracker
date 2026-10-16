import { renderHook } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

const mocks = vi.hoisted(() => ({
	logout: vi.fn(),
	clear: vi.fn(),
	navigate: vi.fn(() => Promise.resolve()),
	endOidcSessionCall: vi.fn(),
	assign: vi.fn(),
}));

vi.mock("@/services/authService", () => ({
	endOidcSessionCall: () => mocks.endOidcSessionCall(),
}));

vi.mock("@/hooks/useAuth", () => ({
	useAuth: (selector: (state: { logout: () => Promise<void> }) => unknown) => selector({ logout: mocks.logout }),
}));

vi.mock("@tanstack/react-query", () => ({
	useQueryClient: () => ({ clear: mocks.clear }),
}));

vi.mock("@tanstack/react-router", () => ({
	useNavigate: () => mocks.navigate,
}));

import { useLogout } from "./useLogout";

describe("useLogout", () => {
	beforeEach(() => {
		vi.clearAllMocks();
		mocks.endOidcSessionCall.mockResolvedValue(null);
		vi.stubGlobal("location", { assign: mocks.assign });
	});

	afterEach(() => {
		vi.unstubAllGlobals();
	});

	it("clears the cache and goes to the login page after signing out", async () => {
		mocks.logout.mockResolvedValue(undefined);
		const { result } = renderHook(() => useLogout());

		await result.current();

		expect(mocks.clear).toHaveBeenCalledTimes(1);
		expect(mocks.navigate).toHaveBeenCalledWith({ to: "/login", replace: true });
	});

	it("still clears the cache and leaves when the server cannot be reached", async () => {
		mocks.logout.mockRejectedValue(new Error("Network error"));
		const { result } = renderHook(() => useLogout());

		await expect(result.current()).resolves.toBeUndefined();

		expect(mocks.clear).toHaveBeenCalledTimes(1);
		expect(mocks.navigate).toHaveBeenCalledWith({ to: "/login", replace: true });
	});

	it("ends the identity provider session when the sign-in came from single sign-on", async () => {
		const order: string[] = [];
		mocks.endOidcSessionCall.mockImplementation(() => {
			order.push("idp");
			return Promise.resolve("https://sso.example.com/logout?id_token_hint=t");
		});
		mocks.logout.mockImplementation(() => {
			order.push("local");
			return Promise.resolve();
		});
		const { result } = renderHook(() => useLogout());

		await result.current();

		expect(order).toEqual(["idp", "local"]);
		expect(mocks.clear).toHaveBeenCalledTimes(1);
		expect(mocks.assign).toHaveBeenCalledWith("https://sso.example.com/logout?id_token_hint=t");
		expect(mocks.navigate).not.toHaveBeenCalled();
	});

	it("still signs out locally when the identity provider sign-out address cannot be fetched", async () => {
		mocks.endOidcSessionCall.mockRejectedValue(new Error("Network error"));
		mocks.logout.mockResolvedValue(undefined);
		const { result } = renderHook(() => useLogout());

		await result.current();

		expect(mocks.logout).toHaveBeenCalledTimes(1);
		expect(mocks.assign).not.toHaveBeenCalled();
		expect(mocks.navigate).toHaveBeenCalledWith({ to: "/login", replace: true });
	});
});
