import {CreationResult, Settings} from '../models';
import {axiosInstance} from './axiosInstance';

const domain = 'settings';

export const getSettings = (): Promise<Settings> => {
  return axiosInstance
    .get<Settings>(domain)
    .then((response) => {
      return response.data;
    });
};

export const saveSettings = (settings: Settings): Promise<CreationResult<Settings>> => {
  return axiosInstance
    .put<CreationResult<Settings>>(domain, { ...settings })
    .then((response) => {
      return response.data;
    });
};
