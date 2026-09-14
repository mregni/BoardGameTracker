import { beforeEach, describe, expect, it, vi } from "vitest";
import type { OidcDiscoveryResult, OidcProviderConfig, OidcSetup } from "@/models";
import { renderWithProviders, screen, userEvent, waitFor } from "@/test/test-utils";

const mocks = vi.hoisted(() => ({
	state: {} as {
		isLoading: boolean;
		provider: OidcProviderConfig | null;
		setup: OidcSetup | undefined;
		save: ReturnType<typeof vi.fn>;
		isSaving: boolean;
		remove: ReturnType<typeof vi.fn>;
		isDeleting: boolean;
		testDiscovery: ReturnType<typeof vi.fn>;
		isTestingDiscovery: boolean;
		discovery: OidcDiscoveryResult | null;
		discoveryError: string | null;
	},
}));

vi.mock("../-hooks/useSsoSettings", () => ({
	useSsoSettings: () => mocks.state,
}));

import { SsoSettings } from "./SsoSettings";

const setup: OidcSetup = {
	publicBaseUrl: "https://games.example.com",
	publicUrlConfigured: true,
	callbackUriTemplate: "https://games.example.com/api/auth/oidc/{name}/callback",
	linkCallbackUriTemplate: "https://games.example.com/api/auth/oidc/{name}/link-callback",
};

const provider: OidcProviderConfig = {
	id: 3,
	name: "keycloak",
	displayName: "Company login",
	enabled: true,
	authority: "https://sso.example.com/realms/games",
	clientId: "bgt",
	hasClientSecret: true,
	scopes: "openid profile email",
	autoProvisionUsers: true,
	authorizationEndpoint: null,
	tokenEndpoint: null,
	userInfoEndpoint: null,
	usernameClaimType: null,
	emailClaimType: null,
	displayNameClaimType: null,
	rolesClaimType: "groups",
	adminGroupValue: "bgt-admins",
	iconUrl: null,
	buttonColor: null,
};

describe("SsoSettings", () => {
	beforeEach(() => {
		mocks.state = {
			isLoading: false,
			provider: null,
			setup,
			save: vi.fn().mockResolvedValue(undefined),
			isSaving: false,
			remove: vi.fn().mockResolvedValue(undefined),
			isDeleting: false,
			testDiscovery: vi.fn(),
			isTestingDiscovery: false,
			discovery: null,
			discoveryError: null,
		};
	});

	it("shows the redirect URIs for the name being typed", async () => {
		const user = userEvent.setup();
		renderWithProviders(<SsoSettings />);

		await user.type(screen.getByLabelText("sso.fields.name.label"), "authentik");

		expect(screen.getByText("https://games.example.com/api/auth/oidc/authentik/callback")).toBeInTheDocument();
		expect(screen.getByText("https://games.example.com/api/auth/oidc/authentik/link-callback")).toBeInTheDocument();
		expect(screen.queryByText("sso.setup.public-url-warning")).not.toBeInTheDocument();
	});

	it("warns when the public URL is derived from the request", () => {
		mocks.state.setup = { ...setup, publicUrlConfigured: false };
		renderWithProviders(<SsoSettings />);

		expect(screen.getByText("sso.setup.public-url-warning")).toBeInTheDocument();
	});

	it("submits the parsed provider and keeps the name read-only once configured", async () => {
		const user = userEvent.setup();
		mocks.state.provider = provider;
		renderWithProviders(<SsoSettings />);

		expect(screen.getByLabelText("sso.fields.name.label")).toBeDisabled();
		expect(screen.getByPlaceholderText("sso.fields.client-secret.unchanged")).toBeInTheDocument();
		await user.clear(screen.getByLabelText("sso.fields.display-name.label"));
		await user.type(screen.getByLabelText("sso.fields.display-name.label"), "Work login");
		await user.click(screen.getByRole("button", { name: "sso.save" }));

		await waitFor(() => expect(mocks.state.save).toHaveBeenCalledTimes(1));
		expect(mocks.state.save.mock.calls[0][0]).toMatchObject({
			name: "keycloak",
			displayName: "Work login",
			clientSecret: "",
			rolesClaimType: "groups",
			usernameClaimType: null,
		});
	});

	it("does not submit an insecure authority", async () => {
		const user = userEvent.setup();
		renderWithProviders(<SsoSettings />);

		await user.type(screen.getByLabelText("sso.fields.name.label"), "idp");
		await user.type(screen.getByLabelText("sso.fields.display-name.label"), "IdP");
		await user.type(screen.getByLabelText("sso.fields.authority.label"), "not a url");
		await user.type(screen.getByLabelText("sso.fields.client-id.label"), "bgt");
		await user.click(screen.getByRole("button", { name: "sso.create" }));

		expect(await screen.findByText("settings:sso.validation.authority")).toBeInTheDocument();
		expect(mocks.state.save).not.toHaveBeenCalled();
	});

	it("tests discovery for the authority in the field and shows the result", async () => {
		const user = userEvent.setup();
		mocks.state.discovery = {
			issuer: "https://other.example.com",
			authorizationEndpoint: "https://sso.example.com/auth",
			tokenEndpoint: "https://sso.example.com/token",
			userInfoEndpoint: "https://sso.example.com/userinfo",
			issuerMatchesAuthority: false,
		};
		renderWithProviders(<SsoSettings />);

		await user.type(screen.getByLabelText("sso.fields.authority.label"), "https://sso.example.com");
		await user.click(screen.getByRole("button", { name: "sso.discovery.button" }));

		expect(mocks.state.testDiscovery).toHaveBeenCalledWith("https://sso.example.com");
		expect(screen.getByText("sso.discovery.issuer-mismatch")).toBeInTheDocument();
		expect(screen.getByText("sso.discovery.token: https://sso.example.com/token")).toBeInTheDocument();
	});

	it("removes the provider after confirmation", async () => {
		const user = userEvent.setup();
		mocks.state.provider = provider;
		renderWithProviders(<SsoSettings />);

		await user.click(screen.getByRole("button", { name: "sso.delete" }));
		await user.click(screen.getByRole("button", { name: "delete.button" }));

		await waitFor(() => expect(mocks.state.remove).toHaveBeenCalledWith(3));
	});
});
