import { QueryClient } from "@tanstack/react-query";
import type { FC, ReactNode } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { useBottomNavStore } from "@/hooks/useHideBottomNav";
import { act, render, screen, waitFor } from "@/test/test-utils";

const mocks = vi.hoisted(() => ({
	navigate: vi.fn(),
	useMatch: vi.fn(),
	useLocation: vi.fn(),
	getSettingsCall: vi.fn(),
	getEnvironmentCall: vi.fn(),
	initSentry: vi.fn(),
	restoreSession: vi.fn(),
	startSessionSync: vi.fn(),
	stopSessionSync: vi.fn(),
	authState: {} as {
		isAuthenticated: boolean;
		authStatus: { authEnabled: boolean } | null;
		fetchAuthStatus: ReturnType<typeof vi.fn>;
	},
	routeContext: {} as { queryClient: import("@tanstack/react-query").QueryClient },
	captured: {} as { Root: FC; Error: FC<{ error: Error; reset: () => void }> },
}));

vi.mock("@tanstack/react-router", () => ({
	createRootRouteWithContext: () => (config: { component: FC; errorComponent: FC }) => {
		mocks.captured.Root = config.component;
		mocks.captured.Error = config.errorComponent;
		return { ...config, useRouteContext: () => mocks.routeContext };
	},
	Outlet: () => <div data-testid="outlet" />,
	useMatch: (...args: unknown[]) => mocks.useMatch(...args),
	useNavigate: () => mocks.navigate,
	useLocation: () => mocks.useLocation(),
}));

vi.mock("@/hooks/useAuth", () => ({
	useAuth: (selector: (state: typeof mocks.authState) => unknown) => selector(mocks.authState),
}));

vi.mock("@/services/settingsService", () => ({
	getSettingsCall: () => mocks.getSettingsCall(),
	getEnvironmentCall: () => mocks.getEnvironmentCall(),
	getLanguagesCall: vi.fn(),
	getVersionInfoCall: vi.fn(),
}));

vi.mock("@/utils/tokenRefresh", () => ({
	restoreSession: () => mocks.restoreSession(),
	startSessionSync: () => mocks.startSessionSync(),
}));

vi.mock("@/utils/sentry", () => ({
	initSentry: () => mocks.initSentry(),
}));

vi.mock("react-error-boundary", () => ({
	ErrorBoundary: ({ children }: { children: ReactNode }) => <>{children}</>,
}));

vi.mock("@/components/ErrorBoundary/ErrorFallback", () => ({
	ErrorFallback: ({ error }: { error: Error }) => <div data-testid="error-fallback">{error.message}</div>,
}));

vi.mock("@/components/NotFound/NotFound", () => ({
	NotFound: () => <div data-testid="not-found" />,
}));

vi.mock("./-components/Sidebar", () => ({
	Sidebar: () => <div data-testid="sidebar" />,
}));

vi.mock("./-components/BottomNav", () => ({
	BottomNav: () => <div data-testid="bottom-nav" />,
}));

vi.mock("./-components/PlayerLinkPrompt", () => ({
	PlayerLinkPrompt: () => <div data-testid="player-link-prompt" />,
}));

import "./__root";

describe("RootComponent", () => {
	beforeEach(() => {
		vi.clearAllMocks();
		mocks.authState = {
			isAuthenticated: false,
			authStatus: { authEnabled: false },
			fetchAuthStatus: vi.fn(() => Promise.resolve(mocks.authState.authStatus)),
		};
		mocks.restoreSession.mockResolvedValue(false);
		mocks.startSessionSync.mockReturnValue(mocks.stopSessionSync);
		mocks.routeContext.queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
		mocks.useMatch.mockReturnValue(null);
		mocks.useLocation.mockReturnValue({ pathname: "/", searchStr: "" });
		mocks.getSettingsCall.mockResolvedValue({ statistics: false });
	});

	describe("Loading state", () => {
		it("should show loading spinner while auth is being checked", () => {
			mocks.authState.fetchAuthStatus = vi.fn(() => new Promise(() => {}));
			const { container } = render(<mocks.captured.Root />);

			expect(container.querySelector(".animate-spin")).toBeInTheDocument();
			expect(screen.queryByTestId("sidebar")).not.toBeInTheDocument();
			expect(screen.queryByTestId("outlet")).not.toBeInTheDocument();
		});

		it("should show layout after auth check completes", async () => {
			render(<mocks.captured.Root />);

			await waitFor(() => {
				expect(screen.getByTestId("sidebar")).toBeInTheDocument();
			});
		});

		it("should show layout even if auth check fails", async () => {
			mocks.authState.fetchAuthStatus = vi.fn().mockRejectedValue(new Error("fail"));
			render(<mocks.captured.Root />);

			await waitFor(() => {
				expect(screen.getByTestId("sidebar")).toBeInTheDocument();
			});
		});
	});

	describe("Player link prompt", () => {
		it("is mounted in the layout for a signed-in user", async () => {
			mocks.authState.isAuthenticated = true;
			render(<mocks.captured.Root />);

			expect(await screen.findByTestId("player-link-prompt")).toBeInTheDocument();
		});

		it("is left out while nobody is signed in", async () => {
			render(<mocks.captured.Root />);

			await screen.findByTestId("sidebar");
			expect(screen.queryByTestId("player-link-prompt")).not.toBeInTheDocument();
		});
	});

	describe("Bottom nav space", () => {
		it("reserves the nav height below the content while the nav is shown", async () => {
			useBottomNavStore.setState({ hideRequests: 0 });
			render(<mocks.captured.Root />);

			const main = await screen.findByRole("main");
			expect(main).toHaveClass("pb-(--bottom-nav-h)", "md:pb-0");
			expect(main.parentElement).toHaveAttribute("data-bottom-nav", "visible");
		});

		it("drops the nav space while a form hides the nav", async () => {
			useBottomNavStore.setState({ hideRequests: 0 });
			render(<mocks.captured.Root />);
			const main = await screen.findByRole("main");

			act(() => useBottomNavStore.getState().requestHide());
			expect(main.parentElement).toHaveAttribute("data-bottom-nav", "hidden");

			act(() => useBottomNavStore.getState().releaseHide());
			expect(main.parentElement).toHaveAttribute("data-bottom-nav", "visible");
		});
	});

	describe("Session restore on boot", () => {
		it("restores the session from the refresh cookie before deciding where to go", async () => {
			mocks.authState.authStatus = { authEnabled: true };
			let finishRestore: (restored: boolean) => void = () => {};
			mocks.restoreSession.mockReturnValue(new Promise<boolean>((resolve) => (finishRestore = resolve)));

			const { container } = render(<mocks.captured.Root />);

			await waitFor(() => expect(mocks.restoreSession).toHaveBeenCalledTimes(1));
			expect(container.querySelector(".animate-spin")).toBeInTheDocument();
			expect(screen.queryByTestId("outlet")).not.toBeInTheDocument();
			expect(mocks.navigate).not.toHaveBeenCalled();

			mocks.authState.isAuthenticated = true;
			finishRestore(true);

			await waitFor(() => expect(screen.getByTestId("outlet")).toBeInTheDocument());
			expect(mocks.navigate).not.toHaveBeenCalled();
		});

		it("sends the visitor to the login page only after the restore failed", async () => {
			mocks.authState.authStatus = { authEnabled: true };

			render(<mocks.captured.Root />);

			await waitFor(() => expect(mocks.navigate).toHaveBeenCalledWith({ to: "/login", search: { redirect: "/" } }));
			expect(mocks.restoreSession).toHaveBeenCalledTimes(1);
		});

		it("does not try to restore a session when auth is disabled", async () => {
			render(<mocks.captured.Root />);

			await waitFor(() => expect(screen.getByTestId("outlet")).toBeInTheDocument());
			expect(mocks.restoreSession).not.toHaveBeenCalled();
			expect(mocks.navigate).not.toHaveBeenCalled();
		});

		it("follows other tabs for as long as the app is mounted", () => {
			const { unmount } = render(<mocks.captured.Root />);

			expect(mocks.startSessionSync).toHaveBeenCalledTimes(1);
			unmount();
			expect(mocks.stopSessionSync).toHaveBeenCalledTimes(1);
		});
	});

	describe("Auth redirect", () => {
		beforeEach(() => {
			mocks.authState.authStatus = { authEnabled: true };
			mocks.authState.isAuthenticated = false;
		});

		it("should redirect to /login when auth is enabled and not authenticated", async () => {
			render(<mocks.captured.Root />);

			await waitFor(() => {
				expect(mocks.navigate).toHaveBeenCalledWith({
					to: "/login",
					search: { redirect: "/" },
				});
			});
		});

		it("should include current path with search string as redirect param", async () => {
			mocks.useLocation.mockReturnValue({ pathname: "/settings", searchStr: "?tab=general" });
			render(<mocks.captured.Root />);

			await waitFor(() => {
				expect(mocks.navigate).toHaveBeenCalledWith({
					to: "/login",
					search: { redirect: "/settings?tab=general" },
				});
			});
		});

		it("should not redirect when already on /login", async () => {
			mocks.useLocation.mockReturnValue({ pathname: "/login", searchStr: "" });
			render(<mocks.captured.Root />);

			await waitFor(() => {
				expect(screen.getByTestId("sidebar")).toBeInTheDocument();
			});
			expect(mocks.navigate).not.toHaveBeenCalled();
		});

		it("should not redirect on bare route", async () => {
			mocks.useMatch.mockReturnValue({});
			render(<mocks.captured.Root />);

			await waitFor(() => {
				expect(screen.getByTestId("outlet")).toBeInTheDocument();
			});
			expect(mocks.navigate).not.toHaveBeenCalled();
		});

		it("should not redirect when auth is disabled", async () => {
			mocks.authState.authStatus = { authEnabled: false };
			render(<mocks.captured.Root />);

			await waitFor(() => {
				expect(screen.getByTestId("sidebar")).toBeInTheDocument();
			});
			expect(mocks.navigate).not.toHaveBeenCalled();
		});

		it("should not redirect when authenticated", async () => {
			mocks.authState.isAuthenticated = true;
			render(<mocks.captured.Root />);

			await waitFor(() => {
				expect(screen.getByTestId("sidebar")).toBeInTheDocument();
			});
			expect(mocks.navigate).not.toHaveBeenCalled();
		});
	});

	describe("Layout", () => {
		it("should render bare layout when on bare route", async () => {
			mocks.useMatch.mockReturnValue({});
			render(<mocks.captured.Root />);

			await waitFor(() => {
				expect(screen.getByTestId("outlet")).toBeInTheDocument();
			});
			expect(screen.queryByTestId("sidebar")).not.toBeInTheDocument();
			expect(screen.queryByTestId("bottom-nav")).not.toBeInTheDocument();
		});

		it("should render full layout with sidebar, outlet, and bottom nav", async () => {
			render(<mocks.captured.Root />);

			await waitFor(() => {
				expect(screen.getByTestId("sidebar")).toBeInTheDocument();
			});
			expect(screen.getByTestId("outlet")).toBeInTheDocument();
			expect(screen.getByTestId("bottom-nav")).toBeInTheDocument();
		});
	});

	describe("Sentry initialization", () => {
		it("should initialize sentry when statistics enabled and auth disabled", async () => {
			mocks.getSettingsCall.mockResolvedValue({ statistics: true });
			render(<mocks.captured.Root />);

			await waitFor(() => {
				expect(mocks.initSentry).toHaveBeenCalled();
			});
		});

		it("should initialize sentry when statistics enabled and authenticated", async () => {
			mocks.authState.authStatus = { authEnabled: true };
			mocks.authState.isAuthenticated = true;
			mocks.getSettingsCall.mockResolvedValue({ statistics: true });
			render(<mocks.captured.Root />);

			await waitFor(() => {
				expect(mocks.initSentry).toHaveBeenCalled();
			});
		});

		it("should not initialize sentry when statistics disabled", async () => {
			mocks.getSettingsCall.mockResolvedValue({ statistics: false });
			render(<mocks.captured.Root />);

			await waitFor(() => {
				expect(screen.getByTestId("sidebar")).toBeInTheDocument();
			});
			expect(mocks.initSentry).not.toHaveBeenCalled();
		});

		it("should not load settings for sentry when not authenticated and auth enabled", async () => {
			mocks.authState.authStatus = { authEnabled: true };
			mocks.authState.isAuthenticated = false;
			render(<mocks.captured.Root />);

			await waitFor(() => {
				expect(mocks.navigate).toHaveBeenCalled();
			});
			expect(mocks.getSettingsCall).not.toHaveBeenCalled();
		});

		it("should never call the admin-only environment endpoint", async () => {
			mocks.authState.authStatus = { authEnabled: true };
			mocks.authState.isAuthenticated = true;
			mocks.getSettingsCall.mockResolvedValue({ statistics: true });
			render(<mocks.captured.Root />);

			await waitFor(() => {
				expect(mocks.initSentry).toHaveBeenCalled();
			});
			expect(mocks.getEnvironmentCall).not.toHaveBeenCalled();
		});
	});

	describe("RouterErrorComponent", () => {
		it("should render ErrorFallback with error props", () => {
			const error = new Error("test error");
			const reset = vi.fn();
			render(<mocks.captured.Error error={error} reset={reset} />);

			expect(screen.getByTestId("error-fallback")).toBeInTheDocument();
			expect(screen.getByText("test error")).toBeInTheDocument();
		});
	});
});
