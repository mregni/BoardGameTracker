import type { FC } from "react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { OidcProvider } from "@/models/Auth/Auth";
import { renderWithProviders, screen, userEvent, waitFor } from "@/test/test-utils";

const mocks = vi.hoisted(() => ({
	navigate: vi.fn(),
	search: {} as { redirect?: string; error?: string; sso?: boolean },
	publicUrl: "",
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
	getSettings: () => ({
		queryKey: ["settings"],
		queryFn: () => Promise.resolve({ emailEnabled: false, publicUrl: mocks.publicUrl }),
	}),
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
		mocks.publicUrl = "";
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

	describe("single sign-on host", () => {
		const page = { origin: "http://localhost:5444", href: "http://localhost:5444/login" };

		beforeEach(() => {
			page.href = "http://localhost:5444/login";
			vi.stubGlobal("location", page);
		});

		afterEach(() => {
			vi.unstubAllGlobals();
		});

		it("moves to the https public URL before starting single sign-on, keeping only a local redirect", async () => {
			const user = userEvent.setup();
			mocks.publicUrl = "https://games.example.com/";
			mocks.search = { redirect: "/games/3" };
			renderWithProviders(<mocks.captured.Page />);

			await user.click(await screen.findByRole("button", { name: "Company login" }));

			expect(page.href).toBe("https://games.example.com/login?redirect=%2Fgames%2F3&sso=true");
		});

		it("drops an external redirect when moving to the public URL", async () => {
			const user = userEvent.setup();
			mocks.publicUrl = "https://games.example.com";
			mocks.search = { redirect: "https://evil.example" };
			renderWithProviders(<mocks.captured.Page />);

			await user.click(await screen.findByRole("button", { name: "Company login" }));

			expect(page.href).toBe("https://games.example.com/login?redirect=%2F&sso=true");
		});

		it("starts single sign-on here when this already is the public URL, or the public URL is not https", async () => {
			const user = userEvent.setup();
			mocks.publicUrl = "http://192.168.1.20:5444";
			mocks.search = { redirect: "/games" };
			renderWithProviders(<mocks.captured.Page />);

			await user.click(await screen.findByRole("button", { name: "Company login" }));

			expect(page.href).toBe("/api/auth/oidc/login?redirect=%2Fgames");
		});

		it("continues the sign-on it was sent here for on the public URL", async () => {
			mocks.publicUrl = "http://localhost:5444";
			mocks.search = { redirect: "/games", sso: true };
			renderWithProviders(<mocks.captured.Page />);

			await waitFor(() => expect(page.href).toBe("/api/auth/oidc/login?redirect=%2Fgames"));
		});

		it("does not start single sign-on by itself on another address than the public URL", async () => {
			mocks.publicUrl = "https://games.example.com";
			mocks.search = { sso: true };
			renderWithProviders(<mocks.captured.Page />);

			await screen.findByRole("button", { name: "Company login" });
			await new Promise((resolve) => setTimeout(resolve, 50));
			expect(page.href).toBe("http://localhost:5444/login");
		});

		it("does not start single sign-on by itself when it cannot work over this connection", async () => {
			mocks.provider = { ...provider, httpsRequired: true };
			mocks.search = { sso: true };
			renderWithProviders(<mocks.captured.Page />);

			await screen.findByRole("button", { name: "Company login" });
			await new Promise((resolve) => setTimeout(resolve, 50));
			expect(page.href).toBe("http://localhost:5444/login");
		});
	});
});
