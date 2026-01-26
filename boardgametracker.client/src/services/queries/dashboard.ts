import { getStatistics } from '../dashboardService';

import { createEntityQueryWithKeys } from './queryFactory';

import { QUERY_KEYS } from '@/models';

export const getDashboardStatistics = createEntityQueryWithKeys(
  [QUERY_KEYS.dashboard, QUERY_KEYS.statistics],
  getStatistics
);
