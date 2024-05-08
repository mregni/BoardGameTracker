import { useQuery } from '@tanstack/react-query';

import { KeyValuePair, QUERY_KEYS, Result } from '../models';
import { getCounts } from './services/countService';

export interface RemoteCounts {
  counts: KeyValuePair<string, number>[] | undefined;
  isPending: boolean;
  isError: boolean;
}

export const useCounts = (): RemoteCounts => {
  const { data, isError, isPending } = useQuery<Result<KeyValuePair<string, number>[]>>({
    queryKey: [QUERY_KEYS.counts],
    queryFn: ({ signal }) => getCounts(signal),
  });

  return {
    counts: data?.model,
    isPending,
    isError,
  };
};
