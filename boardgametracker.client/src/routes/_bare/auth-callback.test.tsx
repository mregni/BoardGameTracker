import type { FC, ReactNode } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { renderWithTheme, screen, waitFor } from "@/test/test-utils";

const mocks = vi.hoisted(() => ({
	navigate: vi.fn(),
	search: {} as { error?: string; redirect?: string; linked?: string },
	adoptOidcLoginCall: vi.fn(),
	setSession: vi.fn(),
	isAuthenticated: false,
	successToast: vi.fn(),
	announceSessionChange: vi.fn(),
	captured: {} as { Page: FC },
}));

vi.mock("@tanstack/react-router", () => ({
	createFileRoute: () => (config: { component: FC }) => {
		mocks.captured.Page = config.component;
		return { ...config, useSearch: () => mocks.search };
	},
	Link: ({ children, to, search }: { children: ReactNode; to: string; search?: Record<string, string> }) => (
		<a href={search ? `${to}?${new URLSearchParams(search)}` : to}>{children}</a>
	),
	useNavigate: () => mocks.navigate,
}));

vi.mock("@/services/authService", () => ({
	adoptOidcLoginCall: () => mocks.adoptOidcLoginCall(),
}));

vi.mock("@/hooks/useAuth", () => ({
	useAuth: (selector: (state: { setSession: typeof mocks.setSession; isAuthenticated: boolean }) => unknown) =>
		selector({ setSession: mocks.setSession, isAuthenticated: mocks.isAuthenticated }),
}));

vi.mock("@/utils/sessionSync", () => ({
	announceSessionChange: (change: string) => mocks.announceSessionChange(change),
}));

vi.mock("@/routes/-hooks/useToasts", () => ({
	useToasts: () => ({ successToast: (key: string) => mocks.successToast(key), errorToast: vi.fn() }),
}));

const translations: Record<string, string> = {
	"error:auth.oidc-provider-rejected": "The identity provider refused the sign-in.",
	"error:auth.oidc-handoff-expired": "The sign-in took too long to complete. Please try again.",
	"error:auth.oidc-failed": "Signing in with the identity provider failed.",
	"error:auth.oidc-username-taken":
		"An account with this username already exists. Sign in with your password and link SSO under Settings → Account.",
	"error:auth.oidc-id-token-invalid": "The identity provider's sign-in token could not be verified.",
	"error:auth.account-disabled": "This account is disabled. Ask your administrator to enable it.",
	"error:auth.oidc-https-required":
		"Single sign-on requires HTTPS. Put BoardGameTracker behind a reverse proxy with TLS and set an https public URL.",
};

vi.mock("@/utils/i18n", () => ({
	default: {
		t: (key: string, options?: { defaultValue?: string }) => translations[key] ?? options?.defaultValue ?? key,
		exists: (key: string) => key in translations,
	},
}));

import "./auth-callback";

const login = {
	accessToken: "access",
	expiresAt: new Date(),
	user: { id: "u1", username: "jane", displayName: "Jane", roles: ["User"] },
};

describe("AuthCallbackPage", () => {
	beforeEach(() => {
		vi.clearAllMocks();
		mocks.search = {};
		mocks.isAuthenticated = false;
	});

	it("adopts the handed-off login and continues to the requested page", async () => {
		mocks.search = { redirect: "/games/3" };
		mocks.adoptOidcLoginCall.mockResolvedValue(login);

		renderWithTheme(<mocks.captured.Page />);

		await waitFor(() => expect(mocks.setSession).toHaveBeenCalledWith("access", login.user));
		expect(mocks.announceSessionChange).toHaveBeenCalledWith("login");
		expect(mocks.navigate).toHaveBeenCalledWith({ to: "/games/3", replace: true });
	});

	it("spends the single-use handoff once when the page re-renders before it resolves", async () => {
		mocks.search = { redirect: "/games/3" };
		let resolveAdopt: (value: typeof login) => void = () => {};
		mocks.adoptOidcLoginCall.mockReturnValue(
			new Promise<typeof login>((resolve) => {
				resolveAdopt = resolve;
			}),
		);

		const { rerender } = renderWithTheme(<mocks.captured.Page />);
		rerender(<mocks.captured.Page />);
		resolveAdopt(login);

		await waitFor(() => expect(mocks.setSession).toHaveBeenCalledTimes(1));
		expect(mocks.adoptOidcLoginCall).toHaveBeenCalledTimes(1);
		expect(mocks.navigate).toHaveBeenCalledTimes(1);
		expect(screen.queryByText("The sign-in took too long to complete. Please try again.")).not.toBeInTheDocument();
	});

	it("never follows an external redirect", async () => {
		mocks.search = { redirect: "https://evil.example.com" };
		mocks.adoptOidcLoginCall.mockResolvedValue(login);

		renderWithTheme(<mocks.captured.Page />);

		await waitFor(() => expect(mocks.navigate).toHaveBeenCalledWith({ to: "/", replace: true }));
	});

	it("shows the translated error and a way back when the provider flow failed", async () => {
		mocks.search = { error: "error.auth.oidc-provider-rejected" };

		renderWithTheme(<mocks.captured.Page />);

		expect(await screen.findByText("The identity provider refused the sign-in.")).toBeInTheDocument();
		expect(screen.getByRole("link")).toHaveAttribute("href", "/login");
		expect(mocks.adoptOidcLoginCall).not.toHaveBeenCalled();
		expect(mocks.navigate).not.toHaveBeenCalled();
	});

	it.each(["<script>alert(1)</script>", "Your account is locked, call 0800 123 456", "error.auth.made-up-key"])(
		"shows the generic failure instead of text from the address (%s)",
		async (error) => {
			mocks.search = { error };

			renderWithTheme(<mocks.captured.Page />);

			expect(await screen.findByText("Signing in with the identity provider failed.")).toBeInTheDocument();
			expect(screen.queryByText(error)).not.toBeInTheDocument();
		},
	);

	it("explains that single sign-on needs https when the callback arrived over plain http", async () => {
		mocks.search = { error: "error.auth.oidc-https-required" };

		renderWithTheme(<mocks.captured.Page />);

		expect(
			await screen.findByText(
				"Single sign-on requires HTTPS. Put BoardGameTracker behind a reverse proxy with TLS and set an https public URL.",
			),
		).toBeInTheDocument();
		expect(mocks.adoptOidcLoginCall).not.toHaveBeenCalled();
	});

	it("sends a signed-in user back to the account settings when linking a provider failed", async () => {
		mocks.search = { error: "error.auth.oidc-already-linked" };
		mocks.isAuthenticated = true;

		renderWithTheme(<mocks.captured.Page />);

		const link = await screen.findByRole("link");
		expect(link).toHaveAttribute("href", "/settings?tab=account");
		expect(link).toHaveTextContent("back-to-settings");
		expect(mocks.adoptOidcLoginCall).not.toHaveBeenCalled();
	});

	it("explains an expired handoff instead of looping back to login", async () => {
		mocks.adoptOidcLoginCall.mockRejectedValue(new Error("401"));

		renderWithTheme(<mocks.captured.Page />);

		expect(await screen.findByText("The sign-in took too long to complete. Please try again.")).toBeInTheDocument();
		expect(mocks.setSession).not.toHaveBeenCalled();
	});

	it("confirms a linked provider and returns to the account settings", async () => {
		mocks.search = { linked: "fake" };

		renderWithTheme(<mocks.captured.Page />);

		await waitFor(() =>
			expect(mocks.navigate).toHaveBeenCalledWith({ to: "/settings", search: { tab: "account" }, replace: true }),
		);
		expect(mocks.successToast).toHaveBeenCalledWith("auth:oidc.linked");
		expect(mocks.adoptOidcLoginCall).not.toHaveBeenCalled();
	});

	it("explains a username clash and sends the user to sign in with their password", async () => {
		mocks.search = { error: "error.auth.oidc-username-taken" };
		mocks.isAuthenticated = true;

		renderWithTheme(<mocks.captured.Page />);

		expect(
			await screen.findByText(
				"An account with this username already exists. Sign in with your password and link SSO under Settings → Account.",
			),
		).toBeInTheDocument();
		const link = screen.getByRole("link");
		expect(link).toHaveAttribute("href", "/login");
		expect(link).toHaveTextContent("oidc.sign-in-with-password");
		expect(mocks.adoptOidcLoginCall).not.toHaveBeenCalled();
	});

	it("also sends an email clash to the password sign-in", async () => {
		mocks.search = { error: "error.auth.oidc-email-exists" };

		renderWithTheme(<mocks.captured.Page />);

		const link = await screen.findByRole("link");
		expect(link).toHaveAttribute("href", "/login");
		expect(link).toHaveTextContent("oidc.sign-in-with-password");
	});

	it.each([
		["error.auth.oidc-id-token-invalid", "The identity provider's sign-in token could not be verified."],
		["error.auth.account-disabled", "This account is disabled. Ask your administrator to enable it."],
	])("shows the message for %s with the usual way back", async (error, message) => {
		mocks.search = { error };

		renderWithTheme(<mocks.captured.Page />);

		expect(await screen.findByText(message)).toBeInTheDocument();
		expect(screen.getByRole("link")).toHaveTextContent("back-to-login");
	});
});
