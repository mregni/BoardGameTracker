import { axiosInstance } from '../utils/axiosInstance';
import { Settings } from '../models';

import { VersionInfo } from '@/models/Settings/VersionInfo';
import { Language, Environment } from '@/models';

const domain = 'settings';

export const getSettingsCall = (): Promise<Settings> => {
  return axiosInstance.get<Settings>(domain).then((response) => {
    return response.data;
  });
};

export const updateSettingsCall = (settings: Settings): Promise<Settings> => {
  return axiosInstance.put<Settings>(domain, { ...settings }).then((response) => {
    return response.data;
  });
};

export const getLanguagesCall = (): Promise<Language[]> => {
  return axiosInstance.get<Language[]>(`${domain}/languages`).then((response) => {
    return response.data;
  });
};

export const getEnvironmentCall = (): Promise<Environment> => {
  return axiosInstance.get<Environment>(`${domain}/environment`).then((response) => {
    return response.data;
  });
};

export const getVersionInfoCall = (): Promise<VersionInfo> => {
  return axiosInstance.get<VersionInfo>(`${domain}/version-info`).then((response) => {
    return response.data;
  });
};
