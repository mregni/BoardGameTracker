import { AxiosError } from 'axios';
import { UseQueryResult, useQuery } from '@tanstack/react-query';

import { KeyValuePair, FailResult, QUERY_KEYS } from '@/models';
import { getCounts } from '@/hooks/services/countService';

interface Props {
  counts: UseQueryResult<KeyValuePair<string, number>[], AxiosError<FailResult>>;
}

export const useBgtMenuBar = (): Props => {
  const counts = useQuery<KeyValuePair<string, number>[], AxiosError<FailResult>>({
    queryKey: [QUERY_KEYS.counts],
    queryFn: ({ signal }) => getCounts(signal),
  });

  return {
    counts,
  };
};
