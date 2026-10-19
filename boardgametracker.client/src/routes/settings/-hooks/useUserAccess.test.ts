import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { act, renderHook, waitFor } from "@testing-library/react";
import React from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";

const { successToast, errorToast } = vi.hoisted(() => ({
	successToast: vi.fn(),
	errorToast: vi.fn(),
}));

const login = {
	id: 4,
	provider: "keycloak",
	providerKey: "sub-1",
	providerDisplayName: "Jane",
	linkedAt: "2026-09-01T10:00:00Z",
};

vi.mock("@/routes/-hooks/useToasts", () => ({
	useToasts: () => ({ successToast, errorToast }),
}));

vi.mock("@/utils/errorUtils", () => ({
	apiErrorMessage: (_error: unknown, fallbackKey: string) => fallbackKey,
}));

vi.mock("@/services/authService", () => ({
	getUserExternalLoginsCall: vi.fn(),
	disableUserCall: vi.fn(),
	enableUserCall: vi.fn(),
	unlinkUserExternalLoginCall: vi.fn(),
	manageRoleByIdentityProviderCall: vi.fn(),
}));

import {
	disableUserCall,
	enableUserCall,
	getUserExternalLoginsCall,
	manageRoleByIdentityProviderCall,
	unlinkUserExternalLoginCall,
} from "@/services/authService";
import { useUserAccess } from "./useUserAccess";

const createWrapper =
	(queryClient: QueryClient) =>
	({ children }: { children: React.ReactNode }) =>
		React.createElement(QueryClientProvider, { client: queryClient }, children);

const renderUseUserAccess = async () => {
	const queryClient = new QueryClient({
		defaultOptions: { queries: { retry: false, gcTime: 0 }, mutations: { retry: false } },
	});
	const invalidate = vi.spyOn(queryClient, "invalidateQueries");
	const rendered = renderHook(() => useUserAccess("user-2"), { wrapper: createWrapper(queryClient) });
	await waitFor(() => expect(rendered.result.current.isLoading).toBe(false));
	return { ...rendered, invalidate };
};

describe("useUserAccess", () => {
	beforeEach(() => {
		vi.clearAllMocks();
		vi.mocked(getUserExternalLoginsCall).mockResolvedValue([login] as never);
	});

	it("loads the linked identities of the user", async () => {
		const { result } = await renderUseUserAccess();

		expect(getUserExternalLoginsCall).toHaveBeenCalledWith("user-2");
		expect(result.current.logins).toEqual([login]);
	});

	it.each([
		["disable", disableUserCall],
		["enable", enableUserCall],
		["manageRoleByIdentityProvider", manageRoleByIdentityProviderCall],
	] as const)(
		"%s calls the API and refreshes the users without a toast, as the dialog shows the change",
		async (action, call) => {
			vi.mocked(call).mockResolvedValue({} as never);
			const { result, invalidate } = await renderUseUserAccess();

			act(() => result.current[action]());

			await waitFor(() => expect(invalidate).toHaveBeenCalledWith({ queryKey: ["users"] }));
			expect(call).toHaveBeenCalledWith("user-2");
			expect(successToast).not.toHaveBeenCalled();
		},
	);

	it("unlinks one identity of the user", async () => {
		vi.mocked(unlinkUserExternalLoginCall).mockResolvedValue(undefined);
		const { result, invalidate } = await renderUseUserAccess();

		act(() => result.current.unlink(4));

		await waitFor(() => expect(invalidate).toHaveBeenCalledWith({ queryKey: ["users"] }));
		expect(unlinkUserExternalLoginCall).toHaveBeenCalledWith("user-2", 4);
		expect(successToast).not.toHaveBeenCalled();
	});

	it.each([
		["disable", disableUserCall, "settings:account.notifications.user-disable-failed"],
		["enable", enableUserCall, "settings:account.notifications.user-enable-failed"],
		[
			"manageRoleByIdentityProvider",
			manageRoleByIdentityProviderCall,
			"settings:account.notifications.role-manage-failed",
		],
	] as const)("reports a failing %s", async (action, call, toast) => {
		vi.mocked(call).mockRejectedValue(new Error("nope"));
		const { result } = await renderUseUserAccess();

		act(() => result.current[action]());

		await waitFor(() => expect(errorToast).toHaveBeenCalledWith(toast));
		expect(successToast).not.toHaveBeenCalled();
	});

	it("reports a failing unlink", async () => {
		vi.mocked(unlinkUserExternalLoginCall).mockRejectedValue(new Error("nope"));
		const { result } = await renderUseUserAccess();

		act(() => result.current.unlink(4));

		await waitFor(() =>
			expect(errorToast).toHaveBeenCalledWith("settings:account.notifications.external-login-unlink-failed"),
		);
	});
});
