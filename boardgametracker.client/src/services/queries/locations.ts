import { queryOptions } from '@tanstack/react-query';

import { getLocationsCall } from '../locationService';

import { QUERY_KEYS } from '@/models';

export const getLocations = () =>
  queryOptions({
    queryKey: [QUERY_KEYS.locations],
    queryFn: () => getLocationsCall(),
  });
