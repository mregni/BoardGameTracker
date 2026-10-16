import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { act, renderHook, waitFor } from "@testing-library/react";
import React from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { ApiError, OidcProviderRequest } from "@/models";

const { successToast, errorToast } = vi.hoisted(() => ({
	successToast: vi.fn(),
	errorToast: vi.fn(),
}));

vi.mock("@/routes/-hooks/useToasts", () => ({
	useToasts: () => ({ successToast, errorToast }),
}));

vi.mock("@/utils/i18n", () => ({
	default: {
		t: (key: string, options?: { defaultValue?: string }) =>
			key === "error:auth.insecure-authority" ? "Use https for the authority" : (options?.defaultValue ?? key),
	},
}));

vi.mock("@/services/oidcAdminService", () => ({
	getOidcProvidersCall: vi.fn(() => Promise.resolve([])),
	getOidcProviderConfigCall: vi.fn(),
	getOidcSetupCall: vi.fn(() =>
		Promise.resolve({
			publicBaseUrl: "https://games.example.com",
			publicUrlConfigured: true,
			callbackUriTemplate: "https://games.example.com/api/auth/oidc/{name}/callback",
			linkCallbackUriTemplate: "https://games.example.com/api/auth/oidc/{name}/link-callback",
		}),
	),
	testOidcDiscoveryCall: vi.fn(),
	createOidcProviderCall: vi.fn(),
	updateOidcProviderCall: vi.fn(),
	deleteOidcProviderCall: vi.fn(),
}));

import {
	createOidcProviderCall,
	deleteOidcProviderCall,
	getOidcProviderConfigCall,
	getOidcProvidersCall,
	testOidcDiscoveryCall,
	updateOidcProviderCall,
} from "@/services/oidcAdminService";
import { useSsoSettings } from "./useSsoSettings";

const createWrapper = () => {
	const queryClient = new QueryClient({
		defaultOptions: { queries: { retry: false, gcTime: 0 }, mutations: { retry: false } },
	});
	return ({ children }: { children: React.ReactNode }) =>
		React.createElement(QueryClientProvider, { client: queryClient }, children);
};

const request = {
	name: "idp",
	displayName: "IdP",
	authority: "http://idp.example.com",
	clientId: "bgt",
	clientSecret: "",
	scopes: "openid profile email",
	enabled: true,
	autoProvisionUsers: true,
} as OidcProviderRequest;

describe("useSsoSettings", () => {
	beforeEach(() => {
		vi.clearAllMocks();
	});

	it("shows the reason the server gives for refusing a plain http authority", async () => {
		const refused: ApiError = {
			kind: "client",
			status: 400,
			message: "error.auth.insecure-authority",
			url: "/api/auth/oidc/providers",
		};
		vi.mocked(createOidcProviderCall).mockRejectedValue(refused);
		const { result } = renderHook(() => useSsoSettings(), { wrapper: createWrapper() });
		await waitFor(() => expect(result.current.isLoading).toBe(false));

		await act(async () => {
			await result.current.save(request).catch(() => undefined);
		});

		expect(createOidcProviderCall).toHaveBeenCalledWith(request);
		expect(errorToast).toHaveBeenCalledWith("Use https for the authority");
		expect(successToast).not.toHaveBeenCalled();
	});

	it("creates the provider and confirms it when none exists yet", async () => {
		vi.mocked(createOidcProviderCall).mockResolvedValue({} as never);
		const { result } = renderHook(() => useSsoSettings(), { wrapper: createWrapper() });
		await waitFor(() => expect(result.current.isLoading).toBe(false));

		await act(() => result.current.save(request));

		expect(createOidcProviderCall).toHaveBeenCalledWith(request);
		expect(updateOidcProviderCall).not.toHaveBeenCalled();
		expect(successToast).toHaveBeenCalledWith("settings:sso.notifications.saved");
	});

	it("updates the existing provider with its stored configuration", async () => {
		const stored = { id: 3, name: "idp" };
		vi.mocked(getOidcProvidersCall).mockResolvedValueOnce([{ id: 3 }] as never);
		vi.mocked(getOidcProviderConfigCall).mockResolvedValueOnce(stored as never);
		vi.mocked(updateOidcProviderCall).mockResolvedValue({} as never);
		const { result } = renderHook(() => useSsoSettings(), { wrapper: createWrapper() });
		await waitFor(() => expect(result.current.provider).toEqual(stored));

		await act(() => result.current.save(request));

		expect(updateOidcProviderCall).toHaveBeenCalledWith(3, request, stored);
		expect(createOidcProviderCall).not.toHaveBeenCalled();
	});

	it("removes the provider, and reports a failed removal", async () => {
		vi.mocked(deleteOidcProviderCall).mockResolvedValueOnce(undefined).mockRejectedValueOnce(new Error("boom"));
		const { result } = renderHook(() => useSsoSettings(), { wrapper: createWrapper() });
		await waitFor(() => expect(result.current.isLoading).toBe(false));

		await act(() => result.current.remove(3));
		await act(() => result.current.remove(3).catch(() => undefined));

		expect(successToast).toHaveBeenCalledWith("settings:sso.notifications.deleted");
		expect(errorToast).toHaveBeenCalledWith("settings:sso.notifications.delete-failed");
	});

	it("shows the discovery result, and the reason when discovery fails", async () => {
		const discovered = {
			issuer: "https://sso.example.com",
			authorizationEndpoint: "https://sso.example.com/auth",
			tokenEndpoint: "https://sso.example.com/token",
			userInfoEndpoint: "https://sso.example.com/userinfo",
			issuerMatchesAuthority: true,
		};
		vi.mocked(testOidcDiscoveryCall).mockResolvedValueOnce(discovered).mockRejectedValueOnce(new Error("unreachable"));
		const { result } = renderHook(() => useSsoSettings(), { wrapper: createWrapper() });
		await waitFor(() => expect(result.current.isLoading).toBe(false));

		act(() => result.current.testDiscovery("https://sso.example.com"));
		await waitFor(() => expect(result.current.discovery).toEqual(discovered));

		act(() => result.current.testDiscovery("https://down.example.com"));
		await waitFor(() => expect(result.current.discoveryError).toBe("settings:sso.discovery.failed"));
		expect(result.current.discovery).toBeNull();
	});
});
