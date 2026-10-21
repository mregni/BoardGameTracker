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
	callbackUri: "https://games.example.com/api/auth/oidc/callback",
	linkCallbackUri: "https://games.example.com/api/auth/oidc/link-callback",
	postLogoutRedirectUri: "https://games.example.com/login",
	httpsRequired: false,
};

const provider: OidcProviderConfig = {
	id: 3,
	displayName: "Company login",
	enabled: true,
	authority: "https://sso.example.com/realms/games",
	clientId: "bgt",
	hasClientSecret: true,
	scopes: "openid profile email",
	autoProvisionUsers: true,
	defaultRole: "Reader",
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

	it("shows the fixed redirect URIs before anything is typed", () => {
		renderWithProviders(<SsoSettings />);

		expect(screen.queryByLabelText("sso.fields.name.label")).not.toBeInTheDocument();
		expect(screen.getByText("https://games.example.com/api/auth/oidc/callback")).toBeInTheDocument();
		expect(screen.getByText("https://games.example.com/api/auth/oidc/link-callback")).toBeInTheDocument();
		expect(screen.getByText("https://games.example.com/login")).toBeInTheDocument();
		expect(screen.getByText("sso.setup.post-logout-redirect-uri")).toBeInTheDocument();
		expect(screen.queryByText("sso.setup.public-url-warning")).not.toBeInTheDocument();
	});

	it("does not warn about https when single sign-on is usable", () => {
		mocks.state.provider = provider;
		renderWithProviders(<SsoSettings />);

		expect(screen.queryByText("sso.https-required")).not.toBeInTheDocument();
		expect(screen.getByRole("switch", { name: "sso.fields.enabled.label" })).toBeEnabled();
		expect(screen.getByText("sso.fields.enabled.description")).toBeInTheDocument();
	});

	it("names the provider by its button label once it is configured", () => {
		mocks.state.provider = provider;
		renderWithProviders(<SsoSettings />);

		expect(screen.getByText("sso.description.configured")).toBeInTheDocument();
		expect(screen.queryByLabelText("sso.fields.name.label")).not.toBeInTheDocument();
		expect(screen.queryByText("sso.description.inactive")).not.toBeInTheDocument();
	});

	it("shows an enabled provider as configured but inactive without https, and only lets it be switched off", async () => {
		const user = userEvent.setup();
		mocks.state.provider = provider;
		mocks.state.setup = { ...setup, publicBaseUrl: "http://games.example.com", httpsRequired: true };
		renderWithProviders(<SsoSettings />);

		expect(screen.getByText("sso.description.inactive")).toBeInTheDocument();
		expect(screen.getAllByRole("alert")).toHaveLength(1);
		expect(screen.getByRole("alert")).toHaveTextContent("sso.https-required-locked");
		const toggle = screen.getByRole("switch", { name: "sso.fields.enabled.label" });
		expect(toggle).toBeEnabled();
		expect(toggle).toHaveAttribute("aria-checked", "true");
		expect(screen.getByText("sso.fields.enabled.description")).toBeInTheDocument();
		expect(screen.getByRole("button", { name: "sso.save" })).toBeDisabled();

		await user.click(toggle);

		expect(toggle).toHaveAttribute("aria-checked", "false");
		expect(toggle).toBeDisabled();
		const save = screen.getByRole("button", { name: "sso.save" });
		expect(save).toBeEnabled();
		await user.click(save);
		await waitFor(() => expect(mocks.state.save).toHaveBeenCalledTimes(1));
		expect(mocks.state.save.mock.calls[0][0]).toMatchObject({ enabled: false });
	});

	it("greys out the redirect URIs until the public URL is https", () => {
		mocks.state.provider = provider;
		mocks.state.setup = { ...setup, publicUrlConfigured: false, httpsRequired: true };
		renderWithProviders(<SsoSettings />);

		expect(screen.getByText("sso.setup.https-pending")).toBeInTheDocument();
		expect(screen.queryByText("sso.setup.public-url-warning")).not.toBeInTheDocument();
		expect(screen.getByTestId("sso-redirect-uris")).toHaveClass("opacity-50");
		for (const copy of screen.getAllByRole("button", { name: "sso.setup.copy" })) {
			expect(copy).toBeDisabled();
		}
	});

	it("keeps a disabled provider disabled and lets its other fields be saved without https", async () => {
		const user = userEvent.setup();
		mocks.state.provider = { ...provider, enabled: false };
		mocks.state.setup = { ...setup, httpsRequired: true };
		renderWithProviders(<SsoSettings />);

		expect(screen.getByText("sso.description.configured")).toBeInTheDocument();
		expect(screen.getByRole("alert")).toHaveTextContent("sso.https-required");
		expect(screen.getByRole("switch", { name: "sso.fields.enabled.label" })).toHaveAttribute("aria-checked", "false");
		await user.click(screen.getByRole("button", { name: "sso.save" }));

		await waitFor(() => expect(mocks.state.save).toHaveBeenCalledTimes(1));
		expect(mocks.state.save.mock.calls[0][0]).toMatchObject({ enabled: false });
	});

	it("creates a new provider disabled when https is required", async () => {
		const user = userEvent.setup();
		mocks.state.setup = { ...setup, httpsRequired: true };
		renderWithProviders(<SsoSettings />);

		expect(screen.getByRole("alert")).toHaveTextContent("sso.https-required");
		await user.type(screen.getByLabelText("sso.fields.display-name.label"), "Company login");
		await user.type(screen.getByLabelText("sso.fields.authority.label"), "https://sso.example.com/realms/games");
		await user.type(screen.getByLabelText("sso.fields.client-id.label"), "bgt");
		await user.click(screen.getByRole("button", { name: "sso.create" }));

		await waitFor(() => expect(mocks.state.save).toHaveBeenCalledTimes(1));
		expect(mocks.state.save.mock.calls[0][0]).toMatchObject({ displayName: "Company login", enabled: false });
	});

	it("warns when the public URL is derived from the request", () => {
		mocks.state.setup = { ...setup, publicUrlConfigured: false };
		renderWithProviders(<SsoSettings />);

		expect(screen.getByText("sso.setup.public-url-warning")).toBeInTheDocument();
	});

	it("submits the parsed provider including its default role", async () => {
		const user = userEvent.setup();
		mocks.state.provider = provider;
		renderWithProviders(<SsoSettings />);

		expect(screen.getByPlaceholderText("sso.fields.client-secret.unchanged")).toBeInTheDocument();
		await user.clear(screen.getByLabelText("sso.fields.display-name.label"));
		await user.type(screen.getByLabelText("sso.fields.display-name.label"), "Work login");
		await user.click(screen.getByRole("button", { name: "sso.save" }));

		await waitFor(() => expect(mocks.state.save).toHaveBeenCalledTimes(1));
		expect(mocks.state.save.mock.calls[0][0]).not.toHaveProperty("name");
		expect(mocks.state.save.mock.calls[0][0]).toMatchObject({
			displayName: "Work login",
			clientSecret: "",
			rolesClaimType: "groups",
			usernameClaimType: null,
			defaultRole: "Reader",
		});
	});

	it("creates a provider with the User role as default", async () => {
		const user = userEvent.setup();
		renderWithProviders(<SsoSettings />);

		await user.type(screen.getByLabelText("sso.fields.display-name.label"), "IdP");
		await user.type(screen.getByLabelText("sso.fields.authority.label"), "https://idp.example.com");
		await user.type(screen.getByLabelText("sso.fields.client-id.label"), "bgt");
		await user.click(screen.getByRole("button", { name: "sso.create" }));

		await waitFor(() => expect(mocks.state.save).toHaveBeenCalledTimes(1));
		expect(mocks.state.save.mock.calls[0][0]).toMatchObject({ defaultRole: "User" });
	});

	it("warns that linked accounts must link again when the authority of a provider changes", async () => {
		const user = userEvent.setup();
		mocks.state.provider = provider;
		renderWithProviders(<SsoSettings />);

		expect(screen.queryByText("sso.fields.authority.relink-warning")).not.toBeInTheDocument();
		await user.type(screen.getByLabelText("sso.fields.authority.label"), "/");
		expect(screen.queryByText("sso.fields.authority.relink-warning")).not.toBeInTheDocument();

		await user.clear(screen.getByLabelText("sso.fields.authority.label"));
		await user.type(screen.getByLabelText("sso.fields.authority.label"), "https://login.example.org");

		expect(screen.getByText("sso.fields.authority.relink-warning")).toBeInTheDocument();
	});

	it("does not submit an authority that is not a URL", async () => {
		const user = userEvent.setup();
		renderWithProviders(<SsoSettings />);

		await user.type(screen.getByLabelText("sso.fields.display-name.label"), "IdP");
		await user.type(screen.getByLabelText("sso.fields.authority.label"), "not a url");
		await user.type(screen.getByLabelText("sso.fields.client-id.label"), "bgt");
		await user.click(screen.getByRole("button", { name: "sso.create" }));

		expect(await screen.findByText("settings:sso.validation.authority")).toBeInTheDocument();
		expect(mocks.state.save).not.toHaveBeenCalled();
	});

	it("leaves the decision on a plain http authority to the server", async () => {
		const user = userEvent.setup();
		renderWithProviders(<SsoSettings />);

		await user.type(screen.getByLabelText("sso.fields.display-name.label"), "IdP");
		await user.type(screen.getByLabelText("sso.fields.authority.label"), "http://idp.example.com");
		await user.type(screen.getByLabelText("sso.fields.client-id.label"), "bgt");
		await user.click(screen.getByRole("button", { name: "sso.create" }));

		await waitFor(() => expect(mocks.state.save).toHaveBeenCalledTimes(1));
		expect(mocks.state.save.mock.calls[0][0]).toMatchObject({ authority: "http://idp.example.com" });
	});

	it("tests discovery for the authority in the field and shows the result", async () => {
		const user = userEvent.setup();
		mocks.state.discovery = {
			issuer: "https://other.example.com",
			authorizationEndpoint: "https://sso.example.com/auth",
			tokenEndpoint: "https://sso.example.com/token",
			userInfoEndpoint: null,
			endSessionEndpoint: "https://sso.example.com/logout",
			issuerMatchesAuthority: false,
			signingAlgorithms: ["RS256"],
			signingSupported: true,
			signingCertificateExpiredOn: null,
		};
		renderWithProviders(<SsoSettings />);

		await user.type(screen.getByLabelText("sso.fields.authority.label"), "https://sso.example.com");
		await user.click(screen.getByRole("button", { name: "sso.discovery.button" }));

		expect(mocks.state.testDiscovery).toHaveBeenCalledWith("https://sso.example.com");
		expect(screen.getByText("sso.discovery.issuer-mismatch")).toBeInTheDocument();
		expect(screen.getByText("sso.discovery.token: https://sso.example.com/token")).toBeInTheDocument();
		expect(screen.getByText("sso.discovery.userinfo: sso.discovery.none")).toBeInTheDocument();
		expect(screen.getByText("sso.discovery.end-session: https://sso.example.com/logout")).toBeInTheDocument();
		expect(screen.getByText("sso.discovery.signing: RS256")).toBeInTheDocument();
		expect(screen.queryByText("sso.discovery.signing-unsupported")).not.toBeInTheDocument();
		expect(screen.queryByText("sso.discovery.certificate-expired")).not.toBeInTheDocument();
	});

	it("warns when the provider only signs with algorithms that are not accepted", () => {
		mocks.state.discovery = {
			issuer: "https://sso.example.com",
			authorizationEndpoint: "https://sso.example.com/auth",
			tokenEndpoint: "https://sso.example.com/token",
			userInfoEndpoint: null,
			endSessionEndpoint: null,
			issuerMatchesAuthority: true,
			signingAlgorithms: ["HS256"],
			signingSupported: false,
			signingCertificateExpiredOn: null,
		};
		renderWithProviders(<SsoSettings />);

		expect(screen.getByText("sso.discovery.signing: HS256")).toBeInTheDocument();
		expect(screen.getByText("sso.discovery.signing-unsupported")).toBeInTheDocument();
	});

	it("shows the advertised algorithms as not offered when the provider lists none", () => {
		mocks.state.discovery = {
			issuer: "https://sso.example.com",
			authorizationEndpoint: "https://sso.example.com/auth",
			tokenEndpoint: "https://sso.example.com/token",
			userInfoEndpoint: null,
			endSessionEndpoint: null,
			issuerMatchesAuthority: true,
			signingAlgorithms: [],
			signingSupported: true,
			signingCertificateExpiredOn: null,
		};
		renderWithProviders(<SsoSettings />);

		expect(screen.getByText("sso.discovery.signing: sso.discovery.none")).toBeInTheDocument();
		expect(screen.queryByText("sso.discovery.signing-unsupported")).not.toBeInTheDocument();
	});

	it("warns about an expired signing certificate", () => {
		mocks.state.discovery = {
			issuer: "https://sso.example.com",
			authorizationEndpoint: "https://sso.example.com/auth",
			tokenEndpoint: "https://sso.example.com/token",
			userInfoEndpoint: null,
			endSessionEndpoint: null,
			issuerMatchesAuthority: true,
			signingAlgorithms: ["RS256"],
			signingSupported: true,
			signingCertificateExpiredOn: "2026-05-08T12:00:00+00:00",
		};
		renderWithProviders(<SsoSettings dateFormat="yyyy-MM-dd" uiLanguage="en-US" />);

		expect(screen.getByText("sso.discovery.certificate-expired")).toBeInTheDocument();
		expect(screen.queryByText("sso.discovery.signing-unsupported")).not.toBeInTheDocument();
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
