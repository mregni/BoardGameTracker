import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { act, renderHook } from "@testing-library/react";
import type { ReactNode } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { User } from "@/models/Auth/Auth";
import { useAuth } from "./useAuth";
import { useResetQueriesOnIdentityChange } from "./useResetQueriesOnIdentityChange";

const admin: User = {
	id: "u1",
	username: "admin",
	displayName: null,
	roles: ["Admin"],
	profileImage: null,
	needsPlayerLink: false,
};

describe("useResetQueriesOnIdentityChange", () => {
	let queryClient: QueryClient;
	let wrapper: ({ children }: { children: ReactNode }) => ReactNode;

	beforeEach(() => {
		useAuth.getState().clearAuth();
		queryClient = new QueryClient();
		vi.spyOn(queryClient, "resetQueries").mockResolvedValue(undefined);
		wrapper = ({ children }) => <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>;
	});

	it("does nothing on mount", () => {
		renderHook(() => useResetQueriesOnIdentityChange(), { wrapper });

		expect(queryClient.resetQueries).not.toHaveBeenCalled();
	});

	it("resets every query when someone signs in, so data cached for the anonymous visitor is refetched", () => {
		renderHook(() => useResetQueriesOnIdentityChange(), { wrapper });

		act(() => useAuth.getState().setSession("access", admin));

		expect(queryClient.resetQueries).toHaveBeenCalledTimes(1);
	});

	it("ignores a token refresh for the same user and resets again on a role change or sign-out", () => {
		renderHook(() => useResetQueriesOnIdentityChange(), { wrapper });
		act(() => useAuth.getState().setSession("access", admin));

		act(() => useAuth.getState().setSession("access-2", { ...admin }));
		expect(queryClient.resetQueries).toHaveBeenCalledTimes(1);

		act(() => useAuth.getState().setSession("access-3", { ...admin, roles: ["User"] }));
		expect(queryClient.resetQueries).toHaveBeenCalledTimes(2);

		act(() => useAuth.getState().clearAuth());
		expect(queryClient.resetQueries).toHaveBeenCalledTimes(3);
	});
});
