import type { FC, ReactNode } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { renderWithTheme, screen, waitFor } from "@/test/test-utils";

const mocks = vi.hoisted(() => ({
	navigate: vi.fn(),
	search: {} as { error?: string; redirect?: string; linked?: string },
	adoptOidcLoginCall: vi.fn(),
	setTokens: vi.fn(),
	successToast: vi.fn(),
	captured: {} as { Page: FC },
}));

vi.mock("@tanstack/react-router", () => ({
	createFileRoute: () => (config: { component: FC }) => {
		mocks.captured.Page = config.component;
		return { ...config, useSearch: () => mocks.search };
	},
	Link: ({ children, to }: { children: ReactNode; to: string }) => <a href={to}>{children}</a>,
	useNavigate: () => mocks.navigate,
}));

vi.mock("@/services/authService", () => ({
	adoptOidcLoginCall: () => mocks.adoptOidcLoginCall(),
}));

vi.mock("@/hooks/useAuth", () => ({
	useAuth: (selector: (state: { setTokens: typeof mocks.setTokens }) => unknown) =>
		selector({ setTokens: mocks.setTokens }),
}));

vi.mock("@/routes/-hooks/useToasts", () => ({
	useToasts: () => ({ successToast: mocks.successToast, errorToast: vi.fn() }),
}));

const translations: Record<string, string> = {
	"error:auth.oidc-provider-rejected": "The identity provider refused the sign-in.",
	"error:auth.oidc-handoff-expired": "The sign-in took too long to complete. Please try again.",
	"error:auth.oidc-failed": "Signing in with the identity provider failed.",
};

vi.mock("@/utils/i18n", () => ({
	default: {
		t: (key: string, options?: { defaultValue?: string }) => translations[key] ?? options?.defaultValue ?? key,
	},
}));

import "./auth-callback";

const login = {
	accessToken: "access",
	refreshToken: "refresh",
	expiresAt: new Date(),
	user: { id: "u1", username: "jane", displayName: "Jane", roles: ["User"] },
};

describe("AuthCallbackPage", () => {
	beforeEach(() => {
		vi.clearAllMocks();
		mocks.search = {};
	});

	it("adopts the handed-off login and continues to the requested page", async () => {
		mocks.search = { redirect: "/games/3" };
		mocks.adoptOidcLoginCall.mockResolvedValue(login);

		renderWithTheme(<mocks.captured.Page />);

		await waitFor(() => expect(mocks.setTokens).toHaveBeenCalledWith("access", "refresh", login.user));
		expect(mocks.navigate).toHaveBeenCalledWith({ to: "/games/3", replace: true });
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

	it("explains an expired handoff instead of looping back to login", async () => {
		mocks.adoptOidcLoginCall.mockRejectedValue(new Error("401"));

		renderWithTheme(<mocks.captured.Page />);

		expect(await screen.findByText("The sign-in took too long to complete. Please try again.")).toBeInTheDocument();
		expect(mocks.setTokens).not.toHaveBeenCalled();
	});

	it("confirms a linked provider and returns to the settings", async () => {
		mocks.search = { linked: "fake" };

		renderWithTheme(<mocks.captured.Page />);

		await waitFor(() => expect(mocks.navigate).toHaveBeenCalledWith({ to: "/settings", replace: true }));
		expect(mocks.successToast).toHaveBeenCalledWith("auth:oidc.linked");
		expect(mocks.adoptOidcLoginCall).not.toHaveBeenCalled();
	});
});
