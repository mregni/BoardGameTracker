import type { FC } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { OidcProvider } from "@/models/Auth/Auth";
import { renderWithProviders, screen } from "@/test/test-utils";

const mocks = vi.hoisted(() => ({
	navigate: vi.fn(),
	search: {} as { redirect?: string; error?: string },
	provider: null as OidcProvider | null,
	captured: {} as { Page: FC },
}));

vi.mock("@tanstack/react-router", () => ({
	createFileRoute: () => (config: { component: FC }) => {
		mocks.captured.Page = config.component;
		return { ...config, useSearch: () => mocks.search };
	},
	useNavigate: () => mocks.navigate,
}));

vi.mock("@/services/authService", () => ({
	getOidcProviderCall: () => Promise.resolve(mocks.provider),
}));

vi.mock("@/services/queries/settings", () => ({
	getSettings: () => ({ queryKey: ["settings"], queryFn: () => Promise.resolve({ emailEnabled: false }) }),
}));

vi.mock("@/hooks/useAuth", () => ({
	useAuth: (selector: (state: { login: () => Promise<void>; isLoading: boolean }) => unknown) =>
		selector({ login: vi.fn(), isLoading: false }),
}));

const translations: Record<string, string> = {
	"error:auth.oidc-https-required":
		"Single sign-on requires HTTPS. Put BoardGameTracker behind a reverse proxy with TLS and set an https public URL.",
};

vi.mock("@/utils/i18n", () => ({
	default: {
		t: (key: string, options?: { defaultValue?: string }) => translations[key] ?? options?.defaultValue ?? key,
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
	});

	it("shows the single sign-on button when it is usable over this connection", async () => {
		renderWithProviders(<mocks.captured.Page />);

		expect(await screen.findByRole("button", { name: "Company login" })).toBeInTheDocument();
	});

	it("hides the single sign-on button when the connection is not https", async () => {
		mocks.provider = { ...provider, httpsRequired: true };
		renderWithProviders(<mocks.captured.Page />);

		expect(await screen.findByRole("button", { name: "login" })).toBeInTheDocument();
		await new Promise((resolve) => setTimeout(resolve, 0));
		expect(screen.queryByRole("button", { name: "Company login" })).not.toBeInTheDocument();
		expect(screen.queryByText("or-continue-with")).not.toBeInTheDocument();
	});

	it("explains a refused single sign-on start from the error in the address", async () => {
		mocks.search = { error: "error.auth.oidc-https-required" };
		mocks.provider = { ...provider, httpsRequired: true };
		renderWithProviders(<mocks.captured.Page />);

		expect(
			await screen.findByText(
				"Single sign-on requires HTTPS. Put BoardGameTracker behind a reverse proxy with TLS and set an https public URL.",
			),
		).toBeInTheDocument();
	});

	it("shows no error without one in the address", async () => {
		renderWithProviders(<mocks.captured.Page />);

		await screen.findByRole("button", { name: "Company login" });
		expect(screen.queryByText(/requires HTTPS/)).not.toBeInTheDocument();
	});
});
