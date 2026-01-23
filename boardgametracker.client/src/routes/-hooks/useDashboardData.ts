import { useQueries } from '@tanstack/react-query';

import { getSettings } from '@/services/queries/settings';
import { getDashboardStatistics } from '@/services/queries/dashboard';

export const useDashboardData = () => {
  const [statisticsQuery, settingsQuery] = useQueries({
    queries: [getDashboardStatistics(), getSettings()],
  });

  const statistics = statisticsQuery.data;
  const settings = settingsQuery.data;

  const isLoading = statisticsQuery.isLoading || settingsQuery.isLoading;

  return {
    isLoading,
    statistics,
    settings,
  };
};
