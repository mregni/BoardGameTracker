import { queryOptions } from '@tanstack/react-query';

import { getSettingsCall, getEnvironmentCall, getLanguagesCall } from '../settingsService';

import { QUERY_KEYS } from '@/models';

export const getSettings = () =>
  queryOptions({
    queryKey: [QUERY_KEYS.settings],
    queryFn: () => getSettingsCall(),
  });

export const getEnvironment = () =>
  queryOptions({
    queryKey: [QUERY_KEYS.environment],
    queryFn: () => getEnvironmentCall(),
  });

export const getLanguages = () =>
  queryOptions({
    queryKey: [QUERY_KEYS.languages],
    queryFn: () => getLanguagesCall(),
  });
