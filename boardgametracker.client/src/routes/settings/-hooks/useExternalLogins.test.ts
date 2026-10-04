import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { act, renderHook, waitFor } from "@testing-library/react";
import React from "react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

const { successToast, errorToast } = vi.hoisted(() => ({
	successToast: vi.fn(),
	errorToast: vi.fn(),
}));

const login = { id: 4, provider: "keycloak", providerDisplayName: "Company login", linkedAt: "2026-09-01T10:00:00Z" };
const provider = { name: "keycloak", displayName: "Company login", iconUrl: null, buttonColor: null };

vi.mock("@/routes/-hooks/useToasts", () => ({
	useToasts: () => ({ successToast, errorToast }),
}));

vi.mock("@/utils/errorUtils", () => ({
	apiErrorMessage: (_error: unknown, fallbackKey: string) => fallbackKey,
}));

vi.mock("@/services/queries/oidc", () => ({
	getExternalLogins: () => ({ queryKey: ["externalLogins"], queryFn: () => Promise.resolve([login]) }),
	getPublicOidcProvider: () => ({ queryKey: ["oidcProvider"], queryFn: () => Promise.resolve(provider) }),
}));

vi.mock("@/services/authService", () => ({
	startOidcLinkCall: vi.fn(),
	unlinkExternalLoginCall: vi.fn(),
}));

import { startOidcLinkCall, unlinkExternalLoginCall } from "@/services/authService";
import { useExternalLogins } from "./useExternalLogins";

const createWrapper = () => {
	const queryClient = new QueryClient({
		defaultOptions: { queries: { retry: false, gcTime: 0 }, mutations: { retry: false } },
	});
	return ({ children }: { children: React.ReactNode }) =>
		React.createElement(QueryClientProvider, { client: queryClient }, children);
};

const renderUseExternalLogins = async () => {
	const rendered = renderHook(() => useExternalLogins(), { wrapper: createWrapper() });
	await waitFor(() => expect(rendered.result.current.isLoading).toBe(false));
	return rendered;
};

describe("useExternalLogins", () => {
	beforeEach(() => {
		vi.clearAllMocks();
	});

	afterEach(() => {
		vi.unstubAllGlobals();
	});

	it("exposes the linked logins and the provider", async () => {
		const { result } = await renderUseExternalLogins();

		expect(result.current.logins).toEqual([login]);
		expect(result.current.provider).toEqual(provider);
	});

	it("unlinks a login and confirms it", async () => {
		vi.mocked(unlinkExternalLoginCall).mockResolvedValue(undefined);
		const { result } = await renderUseExternalLogins();

		act(() => result.current.unlink(4));

		await waitFor(() =>
			expect(successToast).toHaveBeenCalledWith("settings:account.external-logins.notifications.unlinked"),
		);
		expect(unlinkExternalLoginCall).toHaveBeenCalledWith(4);
	});

	it("shows the failure when unlinking is refused", async () => {
		vi.mocked(unlinkExternalLoginCall).mockRejectedValue(new Error("last sign-in method"));
		const { result } = await renderUseExternalLogins();

		act(() => result.current.unlink(4));

		await waitFor(() =>
			expect(errorToast).toHaveBeenCalledWith("settings:account.external-logins.notifications.unlink-failed"),
		);
	});

	it("sends the browser to the provider to link an account", async () => {
		const location = { href: "http://localhost/settings" };
		vi.stubGlobal("location", location);
		vi.mocked(startOidcLinkCall).mockResolvedValue("https://sso.example.com/authorize?state=abc");
		const { result } = await renderUseExternalLogins();

		act(() => result.current.link("keycloak"));

		await waitFor(() => expect(location.href).toBe("https://sso.example.com/authorize?state=abc"));
		expect(startOidcLinkCall).toHaveBeenCalledWith("keycloak");
	});

	it("shows the failure when linking cannot start", async () => {
		vi.mocked(startOidcLinkCall).mockRejectedValue(new Error("provider down"));
		const { result } = await renderUseExternalLogins();

		act(() => result.current.link("keycloak"));

		await waitFor(() =>
			expect(errorToast).toHaveBeenCalledWith("settings:account.external-logins.notifications.link-failed"),
		);
	});
});
