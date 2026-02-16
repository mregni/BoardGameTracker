import { MutationCache, QueryCache, QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { createRouter, RouterProvider } from "@tanstack/react-router";
import { lazy } from "react";
import { ErrorBoundary } from "react-error-boundary";
import { toast } from "sonner";
import { ErrorFallback } from "./components/ErrorBoundary/ErrorFallback";
import { isApiError } from "./models";
import { routeTree } from "./routeTree.gen";
import i18n from "./utils/i18n";

const TanStackQueryDevtools = import.meta.env.PROD
	? () => null
	: lazy(() =>
			import("@tanstack/react-query-devtools").then((res) => ({
				default: res.ReactQueryDevtools,
			})),
		);

const TanStackRouterDevtools = import.meta.env.PROD
	? () => null
	: lazy(() =>
			import("@tanstack/react-router-devtools").then((res) => ({
				default: res.TanStackRouterDevtools,
			})),
		);

function getErrorToastMessage(error: unknown): string {
	if (!isApiError(error)) {
		return i18n.t("error.something-went-wrong");
	}

	switch (error.kind) {
		case "network":
			return i18n.t("error.network");
		case "timeout":
			return i18n.t("error.timeout");
		case "server":
			return i18n.t("error.server");
		default:
			return i18n.t("error.something-went-wrong");
	}
}

let lastErrorToastTime = 0;
const ERROR_TOAST_DEBOUNCE_MS = 2000;

const queryClient = new QueryClient({
	defaultOptions: {
		queries: {
			gcTime: 60 * 60 * 1000, // 1 hour
			staleTime: 5 * 60 * 1000, // 5 minutes
			retry: false,
		},
	},
	queryCache: new QueryCache({
		onError: (error) => {
			const now = Date.now();
			if (now - lastErrorToastTime < ERROR_TOAST_DEBOUNCE_MS) return;
			lastErrorToastTime = now;
			toast.error(getErrorToastMessage(error));
		},
	}),
	mutationCache: new MutationCache({
		onError: (error, _variables, _context, mutation) => {
			if (mutation.options.onError) return;
			toast.error(getErrorToastMessage(error));
		},
	}),
});
const router = createRouter({
	routeTree,
	defaultPreload: "intent",
	defaultViewTransition: true,
	context: { queryClient },
});

declare module "@tanstack/react-router" {
	interface Register {
		router: typeof router;
	}
}

function AppContainer() {
	return (
		<ErrorBoundary
			FallbackComponent={ErrorFallback}
			onError={(error, errorInfo) => {
				if (import.meta.env.DEV) {
					// biome-ignore lint/suspicious/noConsole: DEV-only error logging for debugging
					console.error("Error caught by boundary:", error, errorInfo);
				}
			}}
			onReset={() => {
				queryClient.clear();
			}}
		>
			<QueryClientProvider client={queryClient}>
				<RouterProvider router={router} context={{ queryClient }} />
				<TanStackQueryDevtools initialIsOpen />
				<TanStackRouterDevtools router={router} />
			</QueryClientProvider>
		</ErrorBoundary>
	);
}

export default AppContainer;
