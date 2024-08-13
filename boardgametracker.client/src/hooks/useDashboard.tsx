import { useQuery } from '@tanstack/react-query';

import { DashboardCharts, DashboardStatistics, QUERY_KEYS, Result } from '../models';

import { getCharts, getStatistics } from './services/dashboardService';

export interface Props {
  statistics: DashboardStatistics | undefined;
  charts: DashboardCharts | undefined;
  isPending: boolean;
  isError: boolean;
}

export const useDashboard = (): Props => {
  const {
    data: stats,
    isError,
    isPending,
  } = useQuery<Result<DashboardStatistics>>({
    queryKey: [QUERY_KEYS.dashboardStatistics],
    queryFn: ({ signal }) => getStatistics(signal),
  });

  const { data: charts } = useQuery<Result<DashboardCharts>>({
    queryKey: [QUERY_KEYS.dashboardCharts],
    queryFn: ({ signal }) => getCharts(signal),
  });

  return {
    statistics: stats?.model,
    charts: charts?.model,
    isPending,
    isError,
  };
};
