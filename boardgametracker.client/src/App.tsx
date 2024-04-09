import {AxiosError} from 'axios';
import {Route, Routes} from 'react-router-dom';

import {QueryCache, QueryClient, QueryClientProvider} from '@tanstack/react-query';

import BgtMenuBar from './components/BgtLayout/BgtMenuBar';
import {useSettings} from './hooks/useSettings';
import {FailResult} from './models';
import {DashboardPage} from './pages/Dashboard/DashboardPage';
import {GameRoutes} from './pages/Games/GameRoutes';
import {PlayerRoutes} from './pages/Players/PlayerRoutes';
import {useToast} from './providers/BgtToastProvider';

function AppContainer() {
  const { showErrorToast } = useToast();

  const queryClient = new QueryClient({
    defaultOptions: {
      queries: {
        gcTime: 60 * 60 * 1000,  // 1 hour
        staleTime: 5 * 60 * 1000, // 5 minutes
        retry: false
      }
    },
    queryCache: new QueryCache({
      onError: (_error, query) => {
        const error = query.state.error as AxiosError<FailResult>;
        const reason = error.response?.data.reason ?? 'common.unknown-error';
        showErrorToast(reason)
      }
    })
  });

  return (
    <QueryClientProvider client={queryClient}>
      <App />
    </QueryClientProvider>
  )
}

function App() {
  const { settings } = useSettings();

  if (!settings) return null;

  return (
    <div className='flex flex-col md:flex-row h-screen text-white'>
      <BgtMenuBar />
      <div className='flex-1 bg-gray-950 py-3 pr-3 pl-3 md:pl-0'>
        <Routes>
          <Route element={<GameRoutes />} path='/games/*' />
          <Route element={<PlayerRoutes />} path='/players/*' />
          <Route element={<DashboardPage />} path='*' />
        </Routes>
      </div>
    </div>
  )
}

export default AppContainer
