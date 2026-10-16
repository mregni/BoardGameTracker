import { beforeEach, describe, expect, it, vi } from "vitest";
import type { OidcProviderConfig, OidcProviderRequest } from "@/models";

const axiosMock = vi.hoisted(() => ({ post: vi.fn(), put: vi.fn() }));

vi.mock("../utils/axiosInstance", () => ({ axiosInstance: axiosMock }));

import { createOidcProviderCall, updateOidcProviderCall } from "./oidcAdminService";

const request: OidcProviderRequest = {
	name: "keycloak",
	displayName: "Company login",
	authority: "https://sso.example.com/realms/games",
	clientId: "bgt",
	clientSecret: "",
	scopes: "openid profile email",
	enabled: true,
	autoProvisionUsers: true,
	usernameClaimType: null,
	emailClaimType: null,
	displayNameClaimType: null,
	rolesClaimType: "groups",
	adminGroupValue: null,
};

const current = {
	authorizationEndpoint: "https://sso.example.com/authorize",
	tokenEndpoint: "https://sso.example.com/token",
	userInfoEndpoint: "https://sso.example.com/userinfo",
	iconUrl: "https://sso.example.com/icon.svg",
	buttonColor: "#2b6cb0",
} satisfies Partial<OidcProviderConfig>;

describe("oidcAdminService", () => {
	beforeEach(() => {
		axiosMock.post.mockReset().mockResolvedValue({ data: {} });
		axiosMock.put.mockReset().mockResolvedValue({ data: {} });
	});

	it("sends a blank client secret as null so the stored secret is kept", async () => {
		await updateOidcProviderCall(3, request, current);

		expect(axiosMock.put.mock.calls[0][1]).toMatchObject({ id: 3, clientSecret: null });
	});

	it("sends a new client secret as typed", async () => {
		await updateOidcProviderCall(3, { ...request, clientSecret: "s3cret" }, current);

		expect(axiosMock.put.mock.calls[0][1]).toMatchObject({ clientSecret: "s3cret" });
	});

	it("keeps the endpoints, icon and colour that can only be set through the API", async () => {
		await updateOidcProviderCall(3, request, current);

		expect(axiosMock.put).toHaveBeenCalledWith("admin/oidc-providers/3", expect.objectContaining(current));
	});

	it("creates a provider without any of the API-only fields", async () => {
		await createOidcProviderCall(request);

		expect(axiosMock.post.mock.calls[0][1]).toMatchObject({
			clientSecret: null,
			authorizationEndpoint: null,
			tokenEndpoint: null,
			userInfoEndpoint: null,
			iconUrl: null,
			buttonColor: null,
		});
	});
});
