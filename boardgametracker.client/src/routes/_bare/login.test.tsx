import type { FC } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { OidcProvider } from "@/models/Auth/Auth";
import { renderWithProviders, screen } from "@/test/test-utils";

const mocks = vi.hoisted(() => ({
	navigate: vi.fn(),
	search: {} as { redirect?: string; error?: string },
	provider: null as OidcProvider | null,
	captured: {} as { Page: FC },
	auth: { isAuthenticated: false, authEnabled: true as boolean | undefined },
}));

vi.mock("@tanstack/react-router", () => ({
	createFileRoute: () => (config: { component: FC }) => {
		mocks.captured.Page = config.component;
		return { ...config, useSearch: () => mocks.search };
	},
	useNavigate: () => mocks.navigate,
	Navigate: ({ to }: { to: string }) => <div data-testid="redirect">{to}</div>,
}));

vi.mock("@/services/authService", () => ({
	getOidcProviderCall: () => Promise.resolve(mocks.provider),
}));

vi.mock("@/services/queries/settings", () => ({
	getSettings: () => ({ queryKey: ["settings"], queryFn: () => Promise.resolve({ emailEnabled: false }) }),
}));

interface AuthState {
	login: () => Promise<void>;
	isLoading: boolean;
	isAuthenticated: boolean;
	authStatus: { authEnabled: boolean } | null;
}

vi.mock("@/hooks/useAuth", () => ({
	useAuth: (selector: (state: AuthState) => unknown) =>
		selector({
			login: vi.fn(),
			isLoading: false,
			isAuthenticated: mocks.auth.isAuthenticated,
			authStatus: mocks.auth.authEnabled === undefined ? null : { authEnabled: mocks.auth.authEnabled },
		}),
}));

const translations: Record<string, string> = {
	"error:auth.oidc-https-required":
		"Single sign-on requires HTTPS. Put BoardGameTracker behind a reverse proxy with TLS and set an https public URL.",
};

vi.mock("@/utils/i18n", () => ({
	default: {
		t: (key: string, options?: { defaultValue?: string }) => translations[key] ?? options?.defaultValue ?? key,
		exists: (key: string) => key in translations,
	},
}));

import "./login";

const provider: OidcProvider = {
	name: "keycloak",
	displayName: "Company login",
	iconUrl: null,
	buttonColor: null,
	httpsRequired: false,
};

describe("LoginPage", () => {
	beforeEach(() => {
		vi.clearAllMocks();
		mocks.search = {};
		mocks.provider = provider;
		mocks.auth = { isAuthenticated: false, authEnabled: true };
	});

	it("shows the single sign-on button when it is usable over this connection", async () => {
		renderWithProviders(<mocks.captured.Page />);

		expect(await screen.findByRole("button", { name: "Company login" })).toBeInTheDocument();
	});

	it("keeps the single sign-on button but disables it with one line when the connection is not https", async () => {
		mocks.provider = { ...provider, httpsRequired: true };
		renderWithProviders(<mocks.captured.Page />);

		expect(await screen.findByRole("button", { name: "Company login" })).toBeDisabled();
		expect(screen.getByText("sso-https-hint")).toBeInTheDocument();
		expect(screen.getByText("or-continue-with")).toBeInTheDocument();
	});

	it("tells the person signing in what to do, not the admin, when single sign-on was refused for https", async () => {
		mocks.search = { error: "error.auth.oidc-https-required" };
		mocks.provider = { ...provider, httpsRequired: true };
		renderWithProviders(<mocks.captured.Page />);

		expect(await screen.findByText("sso-https-error")).toBeInTheDocument();
		expect(screen.queryByText(/reverse proxy/)).not.toBeInTheDocument();
		expect(await screen.findByRole("button", { name: "Company login" })).toBeDisabled();
		expect(screen.queryByText("sso-https-hint")).not.toBeInTheDocument();
	});

	it("still translates other single sign-on errors from the address", async () => {
		mocks.search = { error: "error.auth.oidc-https-required-elsewhere" };
		renderWithProviders(<mocks.captured.Page />);

		expect(await screen.findByText("error:auth.oidc-failed")).toBeInTheDocument();
	});

	it("never shows text from the address as the error", async () => {
		mocks.search = { error: "Your account is locked, call 0800 123 456" };
		renderWithProviders(<mocks.captured.Page />);

		expect(await screen.findByText("error:auth.oidc-failed")).toBeInTheDocument();
		expect(screen.queryByText(/call 0800/)).not.toBeInTheDocument();
	});

	it("translates a known error key from the address", async () => {
		translations["error:auth.account-disabled"] = "This account is disabled.";
		mocks.search = { error: "error.auth.account-disabled" };
		renderWithProviders(<mocks.captured.Page />);

		expect(await screen.findByText("This account is disabled.")).toBeInTheDocument();
	});

	it("sends someone who is already signed in to where they were going", () => {
		mocks.auth = { isAuthenticated: true, authEnabled: true };
		mocks.search = { redirect: "/games" };
		renderWithProviders(<mocks.captured.Page />);

		expect(screen.getByTestId("redirect")).toHaveTextContent("/games");
		expect(screen.queryByRole("button", { name: "login" })).not.toBeInTheDocument();
	});

	it("leaves the sign-in page when sign-in is turned off", () => {
		mocks.auth = { isAuthenticated: false, authEnabled: false };
		renderWithProviders(<mocks.captured.Page />);

		expect(screen.getByTestId("redirect")).toHaveTextContent("/");
	});

	it("shows no error without one in the address", async () => {
		renderWithProviders(<mocks.captured.Page />);

		await screen.findByRole("button", { name: "Company login" });
		expect(screen.queryByText(/requires HTTPS/)).not.toBeInTheDocument();
	});
});
