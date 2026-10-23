import type { QueryClient } from "@tanstack/react-query";
import {
	createRootRouteWithContext,
	type ErrorComponentProps,
	Outlet,
	useLocation,
	useMatch,
	useNavigate,
} from "@tanstack/react-router";
import { useEffect, useState } from "react";
import { ErrorBoundary } from "react-error-boundary";
import { ErrorFallback } from "@/components/ErrorBoundary/ErrorFallback";
import { NotFound } from "@/components/NotFound/NotFound";
import { useAuth } from "@/hooks/useAuth";
import { useIsBottomNavHidden } from "@/hooks/useHideBottomNav";
import type { MenuItem } from "@/models";
import { getSettings } from "@/services/queries/settings";
import { initSentry } from "@/utils/sentry";
import { restoreSession, startSessionSync } from "@/utils/tokenRefresh";
import { BottomNav } from "./-components/BottomNav";
import { PlayerLinkPrompt } from "./-components/PlayerLinkPrompt";
import { Sidebar } from "./-components/Sidebar";

interface RouterContext {
	queryClient: QueryClient;
	routeMetadata?: MenuItem;
}

// Wrapper to adapt ErrorFallback for TanStack Router
function RouterErrorComponent({ error, reset }: Readonly<ErrorComponentProps>) {
	return <ErrorFallback error={error} resetErrorBoundary={reset} />;
}

export const Route = createRootRouteWithContext<RouterContext>()({
	component: RootComponent,
	errorComponent: RouterErrorComponent,
	notFoundComponent: NotFound,
});

function RootComponent() {
	const isBare = useMatch({ from: "/_bare", shouldThrow: false });
	const navigate = useNavigate();
	const location = useLocation();
	const { queryClient } = Route.useRouteContext();
	const isAuthenticated = useAuth((s) => s.isAuthenticated);
	const authStatus = useAuth((s) => s.authStatus);
	const fetchAuthStatus = useAuth((s) => s.fetchAuthStatus);
	const [authChecked, setAuthChecked] = useState(false);
	const bottomNavHidden = useIsBottomNavHidden();

	useEffect(() => startSessionSync(), []);

	useEffect(() => {
		let cancelled = false;
		fetchAuthStatus()
			.then((status) => (status.authEnabled ? restoreSession() : false))
			.catch(() => false)
			.finally(() => {
				if (!cancelled) {
					setAuthChecked(true);
				}
			});
		return () => {
			cancelled = true;
		};
	}, [fetchAuthStatus]);

	useEffect(() => {
		if (!authChecked || !authStatus) return;

		if (authStatus.authEnabled && !isAuthenticated && !isBare && location.pathname !== "/login") {
			const currentPath = location.pathname + location.searchStr;
			navigate({ to: "/login", search: { redirect: currentPath } });
		}
	}, [authChecked, authStatus, isAuthenticated, isBare, navigate, location]);

	useEffect(() => {
		if (!authChecked) return;
		if (!isAuthenticated && authStatus?.authEnabled) return;

		queryClient
			.query(getSettings())
			.then((settings) => {
				if (settings.statistics) {
					initSentry();
				}
			})
			.catch(() => {});
	}, [authChecked, isAuthenticated, authStatus, queryClient]);

	if (!authChecked) {
		return (
			<div className="flex items-center justify-center h-dvh bg-background">
				<div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary" />
			</div>
		);
	}

	if (isBare) {
		return (
			<ErrorBoundary FallbackComponent={ErrorFallback}>
				<Outlet />
			</ErrorBoundary>
		);
	}

	return (
		<div
			data-bottom-nav={bottomNavHidden ? "hidden" : "visible"}
			className="flex w-full text-white bg-background md:h-dvh md:overflow-hidden"
		>
			<Sidebar />

			<main className="flex-1 min-w-0 pb-(--bottom-nav-h) md:pb-0 min-h-dvh md:min-h-0 md:h-dvh md:overflow-auto md:[scrollbar-gutter:stable] bg-background">
				<ErrorBoundary FallbackComponent={ErrorFallback}>
					<Outlet />
				</ErrorBoundary>
			</main>

			<BottomNav />
			{isAuthenticated && <PlayerLinkPrompt />}
		</div>
	);
}
