import { Outlet, createRootRouteWithContext } from '@tanstack/react-router';
import { QueryClient } from '@tanstack/react-query';

import BgtMenuBar from './-components/BgtMenuBar';

import { MenuItem } from '@/models';

interface RouterContext {
  queryClient: QueryClient;
  routeMetadata?: MenuItem;
}

export const Route = createRootRouteWithContext<RouterContext>()({
  component: RootComponent,
});

function RootComponent() {
  return (
    <div className="flex flex-col md:flex-row text-white h-screen">
      <BgtMenuBar />
      <div className="flex-1 bg-custom-gradient flex flex-col md:overflow-y-auto">
        <Outlet />
      </div>
    </div>
  );
}
