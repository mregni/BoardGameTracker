import {useQuery} from '@tanstack/react-query';

import {Settings} from '../models';
import {axiosInstance} from '../utils/axiosInstance';

export interface RemoteSettings {
  settings: Settings | undefined
  isPending: boolean
  isError: boolean
}

const getSettings = (signal: AbortSignal): Promise<Settings> => {
  return axiosInstance
    .get<Settings>('settings', { signal })
    .then((response) => {
      return response.data;
    });
};

export const useRemoteSettings = (): RemoteSettings => {
  const { data: settings, isError, isPending } = useQuery<Settings>({
    queryKey: ['settings'],
    queryFn: ({ signal }) =>
      getSettings(signal),
    gcTime: 60 * 60 * 1000  //1 hour
  });

  return {
    settings,
    isPending,
    isError
  }
}

