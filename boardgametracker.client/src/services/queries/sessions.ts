import { queryOptions } from '@tanstack/react-query';

import { getSessionCall } from '../sessionService';

import { QUERY_KEYS } from '@/models';

export const getSession = (id: string) =>
  queryOptions({
    queryKey: [QUERY_KEYS.sessions, id],
    queryFn: () => getSessionCall(id),
  });
