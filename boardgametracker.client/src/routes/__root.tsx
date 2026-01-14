import { ErrorBoundary } from 'react-error-boundary';
import { Outlet, createRootRouteWithContext, ErrorComponentProps } from '@tanstack/react-router';
import { QueryClient } from '@tanstack/react-query';

import BgtMenuBar from './-components/BgtMenuBar';

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
    <div className="flex flex-col md:flex-row text-white h-screen">
      <BgtMenuBar />
      <div className="flex-1 bg-background flex flex-col md:overflow-y-auto">
        <ErrorBoundary FallbackComponent={ErrorFallback}>
          <Outlet />
        </ErrorBoundary>
      </div>
    </div>
  );
}
