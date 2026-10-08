import { renderHook } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";

const mocks = vi.hoisted(() => ({
	logout: vi.fn(),
	clear: vi.fn(),
	navigate: vi.fn(() => Promise.resolve()),
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
});
