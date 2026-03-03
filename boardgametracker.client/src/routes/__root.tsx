import type { QueryClient } from "@tanstack/react-query";
import {
	createRootRouteWithContext,
	type ErrorComponentProps,
	Outlet,
	useMatch,
	useNavigate,
} from "@tanstack/react-router";
import { useEffect, useState } from "react";
import { ErrorBoundary } from "react-error-boundary";
import { ErrorFallback } from "@/components/ErrorBoundary/ErrorFallback";
import { NotFound } from "@/components/NotFound/NotFound";
import { useAuth } from "@/hooks/useAuth";

import type { MenuItem } from "@/models";
import { BottomNav } from "./-components/BottomNav";
import { Sidebar } from "./-components/Sidebar";

interface RouterContext {
	queryClient: QueryClient;
	routeMetadata?: MenuItem;
}

// Wrapper to adapt ErrorFallback for TanStack Router
function RouterErrorComponent({ error, reset }: ErrorComponentProps) {
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
	const { isAuthenticated, authStatus, fetchAuthStatus } = useAuth();
	const [authChecked, setAuthChecked] = useState(false);

	useEffect(() => {
		fetchAuthStatus()
			.then(() => setAuthChecked(true))
			.catch(() => setAuthChecked(true));
	}, [fetchAuthStatus]);

	useEffect(() => {
		if (!authChecked || !authStatus) return;

		// If auth is enabled and not bypassed, redirect unauthenticated users to login
		if (authStatus.authEnabled && !authStatus.bypassEnabled && !isAuthenticated && !isBare) {
			navigate({ to: "/login" });
		}
	}, [authChecked, authStatus, isAuthenticated, isBare, navigate]);

	if (!authChecked) {
		return (
			<div className="flex items-center justify-center h-screen bg-background">
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
		<div className="flex size-full text-white bg-background">
			<Sidebar />

			<main className="flex-1 overflow-auto pb-20 md:pb-0 h-screen bg-background">
				<ErrorBoundary FallbackComponent={ErrorFallback}>
					<Outlet />
				</ErrorBoundary>
			</main>

			<BottomNav />
		</div>
	);
}
