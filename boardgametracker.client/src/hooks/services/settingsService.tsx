import { axiosInstance } from '../../utils/axiosInstance';
import { Settings } from '../../models';

import { Language } from '@/models/Settings/Language';
import { Environment } from '@/models/Settings/Environment';

const domain = 'settings';

export const getSettings = (signal: AbortSignal): Promise<Settings> => {
  return axiosInstance.get<Settings>(domain, { signal }).then((response) => {
    return response.data;
  });
};

export const updateSettings = (settings: Settings): Promise<Settings> => {
  return axiosInstance.put<Settings>(domain, { ...settings }).then((response) => {
    return response.data;
  });
};

export const getLanguages = (signal: AbortSignal): Promise<Language[]> => {
  return axiosInstance.get<Language[]>(`${domain}/languages`, { signal }).then((response) => {
    return response.data;
  });
};

export const getEnvironment = (signal: AbortSignal): Promise<Environment> => {
  return axiosInstance.get<Environment>(`${domain}/environment`, { signal }).then((response) => {
    return response.data;
  });
};
