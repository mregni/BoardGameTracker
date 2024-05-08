import { useQuery } from '@tanstack/react-query';

import { QUERY_KEYS, Result, Settings } from '../models';
import { getSettings } from './services/settingsService';

export interface RemoteSettings {
  settings: Settings | undefined;
  isPending: boolean;
  isError: boolean;
}

export const useSettings = (): RemoteSettings => {
  const { data, isError, isPending } = useQuery<Result<Settings>>({
    queryKey: [QUERY_KEYS.settings],
    queryFn: ({ signal }) => getSettings(signal),
  });

  return {
    settings: data?.model,
    isPending,
    isError,
  };
};
