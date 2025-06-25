import { useMemo } from 'react';
import { useQueries } from '@tanstack/react-query';

import { getSettings } from '@/services/queries/settings';
import { getDashboardCharts, getDashboardStatistics } from '@/services/queries/dashboard';

export const useDashboardData = () => {
  const [statisticsQuery, chartsQuery, settingsQuery] = useQueries({
    queries: [getDashboardStatistics(), getDashboardCharts(), getSettings()],
  });

  const statistics = useMemo(() => statisticsQuery.data, [statisticsQuery.data]);
  const charts = useMemo(() => chartsQuery.data, [chartsQuery.data]);
  const settings = useMemo(() => settingsQuery.data, [settingsQuery.data]);

  const isLoading = statisticsQuery.isLoading || chartsQuery.isLoading || settingsQuery.isLoading;

  return {
    isLoading,
    statistics,
    charts,
    settings,
  };
};
