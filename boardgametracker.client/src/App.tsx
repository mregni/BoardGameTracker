import { lazy } from 'react';
import { createRouter, RouterProvider } from '@tanstack/react-router';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';

import { routeTree } from './routeTree.gen';

const TanStackQueryDevtools = import.meta.env.PROD
  ? () => null
  : lazy(() =>
      import('@tanstack/react-query-devtools').then((res) => ({
        default: res.ReactQueryDevtools,
      }))
    );

const TanStackRouterDevtools = import.meta.env.PROD
  ? () => null
  : lazy(() =>
      import('@tanstack/react-router-devtools').then((res) => ({
        default: res.TanStackRouterDevtools,
      }))
    );

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
  // const { errorToast } = useToasts();

  return (
    <QueryClientProvider client={queryClient}>
      {/* <App /> */}
      <RouterProvider router={router} context={{ queryClient }} />
      <TanStackQueryDevtools initialIsOpen />
      <TanStackRouterDevtools router={router} />
    </QueryClientProvider>
  );
}

export default AppContainer;
