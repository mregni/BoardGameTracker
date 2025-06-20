import { AxiosError } from 'axios';
import { useQuery } from '@tanstack/react-query';

import { KeyValuePair, FailResult, QUERY_KEYS } from '@/models';
import { getCounts } from '@/hooks/services/countService';

export const useBgtMenuBar = () => {
  const counts = useQuery<KeyValuePair<string, number>[], AxiosError<FailResult>>({
    queryKey: [QUERY_KEYS.counts],
    queryFn: ({ signal }) => getCounts(signal),
  });

  return {
    counts: counts.data,
  };
};
