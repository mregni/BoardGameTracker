import type { QueryClient } from "@tanstack/react-query";
import { createRootRouteWithContext, type ErrorComponentProps, Outlet } from "@tanstack/react-router";
import { ErrorBoundary } from "react-error-boundary";
import { ErrorFallback } from "@/components/ErrorBoundary/ErrorFallback";
import { NotFound } from "@/components/NotFound/NotFound";

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
