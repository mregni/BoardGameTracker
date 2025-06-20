import { AxiosError } from 'axios';
import { useMutation, useQuery } from '@tanstack/react-query';

import { getSettings, getLanguages, updateSettings, getEnvironment } from './services/settingsService';

import { Language } from '@/models/Settings/Language';
import { Environment } from '@/models/Settings/Environment';
import { FailResult, QUERY_KEYS, Settings } from '@/models';

export const useSettings = () => {
  const settings = useQuery<Settings>({
    queryKey: [QUERY_KEYS.settings],
    queryFn: ({ signal }) => getSettings(signal),
  });

  const { mutateAsync: saveSettings, isPending } = useMutation<Settings, AxiosError<FailResult>, Settings>({
    mutationFn: updateSettings,
    onSuccess() {
      settings.refetch();
    },
  });

  const languages = useQuery<Language[]>({
    queryKey: [QUERY_KEYS.environment],
    queryFn: ({ signal }) => getLanguages(signal),
  });

  const environment = useQuery<Environment>({
    queryKey: [QUERY_KEYS.languages],
    queryFn: ({ signal }) => getEnvironment(signal),
  });

  return {
    settings: settings.data,
    isLoading: settings.isLoading,
    isError: settings.isError,
    saveSettings,
    isPending,
    languages: languages.data ?? [],
    environment: environment.data,
  };
};
