import { useQueries } from '@tanstack/react-query';

import { getSettings } from '@/services/queries/settings';
import { getDashboardCharts, getDashboardStatistics } from '@/services/queries/dashboard';

export const useDashboardData = () => {
  const [statisticsQuery, chartsQuery, settingsQuery] = useQueries({
    queries: [getDashboardStatistics(), getDashboardCharts(), getSettings()],
  });

  const statistics = statisticsQuery.data;
  const charts = chartsQuery.data;
  const settings = settingsQuery.data;

  const isLoading = statisticsQuery.isLoading || chartsQuery.isLoading || settingsQuery.isLoading;

  return {
    isLoading,
    statistics,
    charts,
    settings,
  };
};
