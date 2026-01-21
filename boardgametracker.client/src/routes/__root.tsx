import { ErrorBoundary } from 'react-error-boundary';
import { Outlet, createRootRouteWithContext, ErrorComponentProps } from '@tanstack/react-router';
import { QueryClient } from '@tanstack/react-query';

import { Sidebar } from './-components/Sidebar';
import { BottomNav } from './-components/BottomNav';

import { MenuItem } from '@/models';
import { ErrorFallback } from '@/components/ErrorBoundary/ErrorFallback';

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
});

function RootComponent() {
  return (
    <div className="flex size-full text-white">
      <Sidebar />

      <main className="flex-1 overflow-auto pb-20 md:pb-0 h-screen">
        <ErrorBoundary FallbackComponent={ErrorFallback}>
          <Outlet />
        </ErrorBoundary>
      </main>

      <BottomNav />
    </div>
  );
}
