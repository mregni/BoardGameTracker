import { queryOptions } from '@tanstack/react-query';

import { getCountsCall } from '../countService';

import { QUERY_KEYS } from '@/models';

export const getCounts = () =>
  queryOptions({
    queryKey: [QUERY_KEYS.counts],
    queryFn: () => getCountsCall(),
  });
