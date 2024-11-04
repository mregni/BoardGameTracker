import { AxiosError } from 'axios';
import { useQuery, UseQueryResult } from '@tanstack/react-query';

import { DashboardStatistics, QUERY_KEYS, DashboardCharts, FailResult } from '@/models';
import { getStatistics, getCharts } from '@/hooks/services/dashboardService';

interface Props {
  statistics: UseQueryResult<DashboardStatistics, Error>;
  charts: UseQueryResult<DashboardCharts, Error>;
}

export const useDashboardPage = (): Props => {
  const statistics = useQuery<DashboardStatistics, AxiosError<FailResult>>({
    queryKey: [QUERY_KEYS.dashboardStatistics],
    queryFn: ({ signal }) => getStatistics(signal),
  });

  const charts = useQuery<DashboardCharts, AxiosError<FailResult>>({
    queryKey: [QUERY_KEYS.dashboardCharts],
    queryFn: ({ signal }) => getCharts(signal),
  });

  return {
    statistics,
    charts,
  };
};
