import type { FC, ReactNode } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@/test/test-utils";

const mocks = vi.hoisted(() => ({
	navigate: vi.fn(),
	useMatch: vi.fn(),
	useLocation: vi.fn(),
	getEnvironmentCall: vi.fn(),
	initSentry: vi.fn(),
	authState: {} as {
		isAuthenticated: boolean;
		authStatus: { authEnabled: boolean } | null;
		fetchAuthStatus: ReturnType<typeof vi.fn>;
	},
	captured: {} as { Root: FC; Error: FC<{ error: Error; reset: () => void }> },
}));

vi.mock("@tanstack/react-router", () => ({
	createRootRouteWithContext: () => (config: { component: FC; errorComponent: FC }) => {
		mocks.captured.Root = config.component;
		mocks.captured.Error = config.errorComponent;
		return config;
	},
	Outlet: () => <div data-testid="outlet" />,
	useMatch: (...args: unknown[]) => mocks.useMatch(...args),
	useNavigate: () => mocks.navigate,
	useLocation: () => mocks.useLocation(),
}));

vi.mock("@/hooks/useAuth", () => ({
	useAuth: () => mocks.authState,
}));

vi.mock("@/services/settingsService", () => ({
	getEnvironmentCall: () => mocks.getEnvironmentCall(),
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

import "./__root";

describe("RootComponent", () => {
	beforeEach(() => {
		vi.clearAllMocks();
		mocks.authState = {
			isAuthenticated: false,
			authStatus: { authEnabled: false },
			fetchAuthStatus: vi.fn().mockResolvedValue(undefined),
		};
		mocks.useMatch.mockReturnValue(null);
		mocks.useLocation.mockReturnValue({ pathname: "/", searchStr: "" });
		mocks.getEnvironmentCall.mockResolvedValue({ enableStatistics: false });
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
			mocks.getEnvironmentCall.mockResolvedValue({ enableStatistics: true });
			render(<mocks.captured.Root />);

			await waitFor(() => {
				expect(mocks.initSentry).toHaveBeenCalled();
			});
		});

		it("should initialize sentry when statistics enabled and authenticated", async () => {
			mocks.authState.authStatus = { authEnabled: true };
			mocks.authState.isAuthenticated = true;
			mocks.getEnvironmentCall.mockResolvedValue({ enableStatistics: true });
			render(<mocks.captured.Root />);

			await waitFor(() => {
				expect(mocks.initSentry).toHaveBeenCalled();
			});
		});

		it("should not initialize sentry when statistics disabled", async () => {
			mocks.getEnvironmentCall.mockResolvedValue({ enableStatistics: false });
			render(<mocks.captured.Root />);

			await waitFor(() => {
				expect(screen.getByTestId("sidebar")).toBeInTheDocument();
			});
			expect(mocks.initSentry).not.toHaveBeenCalled();
		});

		it("should not call environment API when not authenticated and auth enabled", async () => {
			mocks.authState.authStatus = { authEnabled: true };
			mocks.authState.isAuthenticated = false;
			render(<mocks.captured.Root />);

			await waitFor(() => {
				expect(mocks.navigate).toHaveBeenCalled();
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
