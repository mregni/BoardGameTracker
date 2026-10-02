import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { act, renderHook, waitFor } from "@testing-library/react";
import React from "react";
import { describe, expect, it, vi } from "vitest";
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

import { createOidcProviderCall } from "@/services/oidcAdminService";
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
});
