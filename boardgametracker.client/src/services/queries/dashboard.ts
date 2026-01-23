import { queryOptions } from '@tanstack/react-query';

import { getStatistics } from '../dashboardService';

import { QUERY_KEYS } from '@/models';

export const getDashboardStatistics = () =>
  queryOptions({
    queryKey: [QUERY_KEYS.dashboard, QUERY_KEYS.statistics],
    queryFn: getStatistics,
  });
