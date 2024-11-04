import { useQuery, UseQueryResult } from '@tanstack/react-query';

import { getSettings } from './services/settingsService';

import { QUERY_KEYS, Settings } from '@/models';

export interface RemoteSettings {
  settings: UseQueryResult<Settings, Error>;
}

export const useSettings = (): RemoteSettings => {
  const settings = useQuery<Settings>({
    queryKey: [QUERY_KEYS.settings],
    queryFn: ({ signal }) => getSettings(signal),
  });

  return {
    settings,
  };
};
