import { AxiosError } from 'axios';
import { useQuery, UseQueryResult } from '@tanstack/react-query';

import { Result, DashboardStatistics, QUERY_KEYS, DashboardCharts, FailResult } from '@/models';
import { getStatistics, getCharts } from '@/hooks/services/dashboardService';

interface Props {
  statistics: UseQueryResult<Result<DashboardStatistics>, Error>;
  charts: UseQueryResult<Result<DashboardCharts>, Error>;
}

export const useDashboardPage = (): Props => {
  const statistics = useQuery<Result<DashboardStatistics>, AxiosError<FailResult>>({
    queryKey: [QUERY_KEYS.dashboardStatistics],
    queryFn: ({ signal }) => getStatistics(signal),
  });

  const charts = useQuery<Result<DashboardCharts>, AxiosError<FailResult>>({
    queryKey: [QUERY_KEYS.dashboardCharts],
    queryFn: ({ signal }) => getCharts(signal),
  });

  return {
    statistics,
    charts,
  };
};
