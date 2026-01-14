import { queryOptions } from '@tanstack/react-query';

import { getAllBadgesCall } from '../badgeService';

import { QUERY_KEYS } from '@/models';

export const getBadges = () =>
  queryOptions({
    queryKey: [QUERY_KEYS.badges],
    queryFn: () => getAllBadgesCall(),
  });
