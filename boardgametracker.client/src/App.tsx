import { ErrorBoundary } from 'react-error-boundary';
import { lazy } from 'react';
import { createRouter, RouterProvider } from '@tanstack/react-router';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';

import { routeTree } from './routeTree.gen';
import { ErrorFallback } from './components/ErrorBoundary/ErrorFallback';

const TanStackQueryDevtools = import.meta.env.PROD ? () => null : () => null;

const TanStackRouterDevtools = import.meta.env.PROD ? () => null : () => null;

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      gcTime: 60 * 60 * 1000, // 1 hour
      staleTime: 5 * 60 * 1000, // 5 minutes
      retry: false,
    },
  },
});
const router = createRouter({
  routeTree,
  defaultPreload: 'intent',
  defaultViewTransition: true,
  context: { queryClient },
});

declare module '@tanstack/react-router' {
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
          console.error('Error caught by boundary:', error, errorInfo);
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
